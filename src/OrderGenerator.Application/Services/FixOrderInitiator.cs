using System.Diagnostics;
using FixOrderRouting.SharedKernel.Constants;
using FixOrderRouting.SharedKernel.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.Aggregates.Enumerators;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace OrderGenerator.Application.Services;

public class FixOrderInitiator : MessageCracker, IApplication, IFixOrderInitiator
{
    private readonly ILogger<FixOrderInitiator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Session? _session;
    private SessionID? _sessionId;
    private readonly Dictionary<string, Guid> _clOrdIdToOrderId = new();

    public FixOrderInitiator(ILogger<FixOrderInitiator> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void OnCreate(SessionID sessionId)
    {
        _logger.LogInformation("FIX Initiator session created: {SessionID}", sessionId);
        _sessionId = sessionId;
    }

    public void OnLogon(SessionID sessionId)
    {
        _logger.LogInformation("FIX Initiator logon successful: {SessionID}", sessionId);
        _session = Session.LookupSession(sessionId);
    }

    public void OnLogout(SessionID sessionId)
    {
        _logger.LogInformation("FIX Initiator logout: {SessionID}", sessionId);
        _session = null;
    }

    public void ToAdmin(Message message, SessionID sessionId) { }

    public void ToApp(Message message, SessionID sessionId) { }

    public void FromAdmin(Message message, SessionID sessionId)
    {
    }

    public void FromApp(Message message, SessionID sessionId)
    {
        Crack(message, sessionId);
    }

    public void OnMessage(ExecutionReport execReport, SessionID sessionId)
    {
        var clOrdId = execReport.GetField(new ClOrdID()).Value.ToString();
        var execType = execReport.GetField(new ExecType()).Value.ToString();
        var ordStatus = execReport.GetField(new OrdStatus()).Value.ToString();
        var text = execReport.IsSet(new Text()) ? execReport.GetField(new Text()).Value.ToString() : null;

        using var activity = FixActivitySource.StartReceiveExecutionReport(clOrdId, execType, ordStatus);

        try
        {
            _logger.LogInformation("Received ExecutionReport: ClOrdID={ClOrdID}, ExecType={ExecType}, OrdStatus={OrdStatus}",
                clOrdId, execType, ordStatus);

            if (_clOrdIdToOrderId.TryGetValue(clOrdId, out var orderId))
            {
                UpdateOrderStatusAsync(orderId, execType, text).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ExecutionReport");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }

    private async Task UpdateOrderStatusAsync(Guid orderId, string execType, string? rejectionReason)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderEventRepository = scope.ServiceProvider.GetRequiredService<IOrderEventRepository>();

            var order = await repository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", orderId);
                return;
            }

            if (execType == FixConstants.ExecType.New)
            {
                order.MarkAsAccepted();
                _logger.LogInformation("Order accepted: {OrderId}", orderId);
                await orderEventRepository.AddAsync(
                    OrderEvent.Create(order.Id, order.Id.ToString(), OrderEventType.Accepted));
            }
            else if (execType == FixConstants.ExecType.Rejected)
            {
                var reason = rejectionReason ?? "Order rejected by OrderAccumulator";
                order.MarkAsRejected(reason);
                _logger.LogWarning("Order rejected: {OrderId}, Reason: {Reason}", orderId, rejectionReason);
                await orderEventRepository.AddAsync(
                    OrderEvent.Create(order.Id, order.Id.ToString(), OrderEventType.Rejected, reason));
            }

            await repository.UpdateAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for OrderId: {OrderId}", orderId);
        }
    }

    public async Task SendNewOrderSingleAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (_session == null || _sessionId == null)
        {
            _logger.LogWarning("FIX session not connected, cannot send order");
            throw new InvalidOperationException("FIX session not connected");
        }

        var clOrdId = order.Id.ToString();
        using var activity = FixActivitySource.StartSendNewOrderSingle(
            clOrdId, order.Symbol.Value, order.Side.Value,
            order.Quantity.Value, order.Price.Value);

        try
        {
            var newOrderSingle = new NewOrderSingle();
            newOrderSingle.Set(new ClOrdID(clOrdId));
            newOrderSingle.Set(new Symbol(order.Symbol.Value));
            newOrderSingle.Set(new Side(order.Side.Value == "BUY" ? FixConstants.Side.Buy : FixConstants.Side.Sell));
            newOrderSingle.Set(new OrderQty((int)order.Quantity.Value));
            newOrderSingle.Set(new Price(order.Price.Value));
            newOrderSingle.Set(new OrdType(FixConstants.OrdType.Limit));
            newOrderSingle.Set(new TransactTime(DateTime.UtcNow));

            _clOrdIdToOrderId[clOrdId] = order.Id;

            Session.SendToTarget(newOrderSingle, _sessionId);
            _logger.LogInformation("NewOrderSingle sent: ClOrdID={ClOrdID}, Symbol={Symbol}",
                order.Id, order.Symbol.Value);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending NewOrderSingle: ClOrdID={ClOrdID}", order.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public bool IsConnected => _session != null;
}
