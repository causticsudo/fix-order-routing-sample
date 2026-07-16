using System.Diagnostics;
using FixOrderRouting.SharedKernel.Constants;
using FixOrderRouting.SharedKernel.Diagnostics;
using FixOrderRouting.SharedKernel.Enums;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Aggregates;
using OrderAccumulator.Domain.Services;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using SymbolVO = OrderAccumulator.Domain.ValueObjects.Symbol;
using PriceVO = OrderAccumulator.Domain.ValueObjects.Price;
using QuantityVO = OrderAccumulator.Domain.ValueObjects.Quantity;
using SideVO = OrderAccumulator.Domain.ValueObjects.OrderSide;
using Message = QuickFix.Message;

namespace OrderAccumulator.Worker.FIX;

public class FixOrderListener : MessageCracker, IApplication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FixOrderListener> _logger;

    public FixOrderListener(
        IServiceProvider serviceProvider,
        ILogger<FixOrderListener> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void OnCreate(SessionID sessionId)
    {
        _logger.LogInformation("FIX Acceptor session created: {SessionID}", sessionId);
    }

    public void OnLogon(SessionID sessionId)
    {
        _logger.LogInformation("FIX Acceptor session logon: {SessionID}", sessionId);
    }

    public void OnLogout(SessionID sessionId)
    {
        _logger.LogInformation("FIX Acceptor session logout: {SessionID}", sessionId);
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

    public void OnMessage(NewOrderSingle newOrderSingle, SessionID sessionId)
    {
        var clOrdId = newOrderSingle.GetField(new ClOrdID()).Value;
        var symbolStr = newOrderSingle.GetField(new Symbol()).Value.ToString();

        using var activity = FixActivitySource.StartReceiveNewOrderSingle(clOrdId, symbolStr);

        try
        {
            _logger.LogInformation("Received NewOrderSingle: ClOrdID={ClOrdID}", clOrdId);

            var sideChar = newOrderSingle.GetField(new Side()).Value.ToString();
            var quantityStr = newOrderSingle.GetField(new OrderQty()).Value.ToString();
            var priceStr = newOrderSingle.GetField(new Price()).Value.ToString();

            var quantity = long.Parse(quantityStr);
            var price = decimal.Parse(priceStr);

            var symbol = SymbolVO.Create(symbolStr);
            var isBuy = sideChar == FixConstants.Side.Buy.ToString();
            var side = SideVO.Create(isBuy ? BusinessConstants.Sides.Buy : BusinessConstants.Sides.Sell);
            var qty = QuantityVO.Create(quantity);
            var prc = PriceVO.Create(price);

            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IOrderExecutionRepository>();
                var exposureCalculator = scope.ServiceProvider.GetRequiredService<ExposureCalculator>();
                var exposureCache = scope.ServiceProvider.GetRequiredService<IExposureCache>();

                var canAccept = exposureCalculator.CanAcceptOrderAsync(symbol, side, qty, prc).GetAwaiter().GetResult();

                OrderExecution execution;
                if (canAccept)
                {
                    execution = OrderExecution.CreateAccepted(clOrdId, symbol, side, qty, prc);
                    _logger.LogInformation("Order accepted: ClOrdID={ClOrdID}, Symbol={Symbol}", clOrdId, symbolStr);
                }
                else
                {
                    var rejectionReason = $"Financial exposure would exceed limit of R$ {BusinessConstants.Orders.ExposureLimit:N0}";
                    execution = OrderExecution.CreateRejected(clOrdId, symbol, side, qty, prc, rejectionReason);
                    _logger.LogWarning("Order rejected: ClOrdID={ClOrdID}, Reason={Reason}", clOrdId, rejectionReason);
                }

                repository.AddAsync(execution).GetAwaiter().GetResult();

                var exposure = canAccept
                    ? exposureCalculator.GetExposureAsync(symbolStr).GetAwaiter().GetResult()
                    : 0m;

                FixActivitySource.RecordExposureValidation(activity, canAccept, exposure, BusinessConstants.Orders.ExposureLimit);

                if (canAccept)
                {
                    exposureCache.SetExposureAsync(symbolStr, exposure).GetAwaiter().GetResult();
                    _logger.LogInformation("Updated exposure: Symbol={Symbol}, Exposure={Exposure}", symbolStr, exposure);
                }

                SendExecutionReport(execution, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NewOrderSingle");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private void SendExecutionReport(OrderExecution execution, SessionID sessionId)
    {
        var status = execution.Status == OrderExecutionStatus.Accepted ? "Accepted" : "Rejected";
        using var activity = FixActivitySource.StartSendExecutionReport(
            execution.ClOrdId, status, execution.RejectionReason);

        try
        {
            var execReport = new ExecutionReport();

            execReport.Set(new OrderID(execution.Id.ToString()));
            execReport.Set(new ExecID(Guid.NewGuid().ToString()));
            execReport.Set(new ClOrdID(execution.ClOrdId));
            execReport.Set(new Symbol(execution.Symbol.Value));
            execReport.Set(new Side(execution.Side.IsBuy() ? '1' : '2'));
            execReport.Set(new OrderQty((int)execution.Quantity.Value));
            execReport.Set(new Price(execution.Price.Value));

            if (execution.Status == OrderExecutionStatus.Accepted)
            {
                execReport.Set(new ExecType(FixConstants.ExecType.New[0]));
                execReport.Set(new OrdStatus(FixConstants.OrdStatus.Filled));
                execReport.Set(new LeavesQty(0));
                execReport.Set(new CumQty((int)execution.Quantity.Value));
                execReport.Set(new AvgPx(execution.Price.Value));
                execReport.Set(new Text("Order accepted - Exposure within limit"));
            }
            else
            {
                execReport.Set(new ExecType(FixConstants.ExecType.Rejected[0]));
                execReport.Set(new OrdStatus(FixConstants.OrdStatus.Rejected));
                execReport.Set(new Text(execution.RejectionReason ?? "Order rejected"));
            }

            Session.SendToTarget(execReport, sessionId);
            _logger.LogInformation("ExecutionReport sent: ClOrdID={ClOrdID}, Status={Status}",
                execution.ClOrdId, execution.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ExecutionReport for ClOrdID={ClOrdID}", execution.ClOrdId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }
}
