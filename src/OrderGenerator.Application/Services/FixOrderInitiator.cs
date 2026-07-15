using FixOrderRouting.SharedKernel.Constants;
using Microsoft.Extensions.Logging;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace OrderGenerator.Application.Services;

/// <summary>
/// FIX Initiator that sends NewOrderSingle messages to OrderAccumulator
/// </summary>
public class FixOrderInitiator : MessageCracker, IApplication, IFixOrderInitiator
{
    private readonly ILogger<FixOrderInitiator> _logger;
    private readonly IOrderRepository _orderRepository;
    private Session? _session;
    private SessionID? _sessionID;
    private readonly Dictionary<string, Guid> _clOrdIdToOrderId = new();

    public FixOrderInitiator(ILogger<FixOrderInitiator> logger, IOrderRepository orderRepository)
    {
        _logger = logger;
        _orderRepository = orderRepository;
    }

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("FIX Initiator session created: {SessionID}", sessionID);
        _sessionID = sessionID;
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogInformation("FIX Initiator logon successful: {SessionID}", sessionID);
        _session = Session.LookupSession(sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogInformation("FIX Initiator logout: {SessionID}", sessionID);
        _session = null;
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        _logger.LogDebug("Sending FIX message: {MsgType}", message.GetField(new MsgType()).Obj);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        Crack(message, sessionID);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        Crack(message, sessionID);
    }

    public void OnMessage(ExecutionReport execReport, SessionID sessionID)
    {
        try
        {
            var clOrdId = execReport.GetField(new ClOrdID()).Obj.ToString();
            var execType = execReport.GetField(new ExecType()).Obj.ToString();
            var ordStatus = execReport.GetField(new OrdStatus()).Obj.ToString();
            var text = execReport.IsSet(new Text()) ? execReport.GetField(new Text()).Obj.ToString() : null;

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
        }
    }

    private async Task UpdateOrderStatusAsync(Guid orderId, string execType, string? rejectionReason)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", orderId);
                return;
            }

            if (execType == FixConstants.ExecType.New)
            {
                order.MarkAsAccepted();
                _logger.LogInformation("Order accepted: {OrderId}", orderId);
            }
            else if (execType == FixConstants.ExecType.Rejected)
            {
                order.MarkAsRejected(rejectionReason ?? "Order rejected by OrderAccumulator");
                _logger.LogWarning("Order rejected: {OrderId}, Reason: {Reason}", orderId, rejectionReason);
            }

            await _orderRepository.UpdateAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for OrderId: {OrderId}", orderId);
        }
    }

    public async Task SendNewOrderSingleAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (_session == null || _sessionID == null)
        {
            _logger.LogWarning("FIX session not connected, cannot send order");
            throw new InvalidOperationException("FIX session not connected");
        }

        try
        {
            var clOrdId = order.Id.ToString();
            var nos = new NewOrderSingle();
            nos.Set(new ClOrdID(clOrdId));
            nos.Set(new Symbol(order.Symbol.Value));
            nos.Set(new Side(order.Side.Value == "BUY" ? FixConstants.Side.Buy : FixConstants.Side.Sell));
            nos.Set(new OrderQty((int)order.Quantity.Value));
            nos.Set(new Price(order.Price.Value));
            nos.Set(new OrdType(FixConstants.OrdType.Limit));
            nos.Set(new TransactTime(DateTime.UtcNow));

            _clOrdIdToOrderId[clOrdId] = order.Id;

            Session.SendToTarget(nos, _sessionID);
            _logger.LogInformation("NewOrderSingle sent: ClOrdID={ClOrdID}, Symbol={Symbol}",
                order.Id, order.Symbol.Value);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending NewOrderSingle: ClOrdID={ClOrdID}", order.Id);
            throw;
        }
    }

    public bool IsConnected => _session != null;
}
