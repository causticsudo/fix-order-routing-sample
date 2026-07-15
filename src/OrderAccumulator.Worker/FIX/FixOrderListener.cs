using FixOrderRouting.SharedKernel.Constants;
using FixOrderRouting.SharedKernel.Enums;
using Microsoft.Extensions.Logging;
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

/// <summary>
/// FIX message listener - receives NewOrderSingle and sends ExecutionReport
/// </summary>
public class FixOrderListener : MessageCracker, IApplication
{
    private readonly IOrderExecutionRepository _repository;
    private readonly ExposureCalculator _exposureCalculator;
    private readonly ILogger<FixOrderListener> _logger;

    public FixOrderListener(
        IOrderExecutionRepository repository,
        ExposureCalculator exposureCalculator,
        ILogger<FixOrderListener> logger)
    {
        _repository = repository;
        _exposureCalculator = exposureCalculator;
        _logger = logger;
    }

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("FIX Acceptor session created: {SessionID}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogInformation("FIX Acceptor session logon: {SessionID}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogInformation("FIX Acceptor session logout: {SessionID}", sessionID);
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

    public void OnMessage(NewOrderSingle nos, SessionID sessionID)
    {
        try
        {
            var clOrdId = nos.GetField(new ClOrdID()).Obj;
            _logger.LogInformation("Received NewOrderSingle: ClOrdID={ClOrdID}", clOrdId);

            var symbolStr = nos.GetField(new Symbol()).Obj.ToString();
            var sideChar = nos.GetField(new Side()).Obj.ToString();
            var quantityStr = nos.GetField(new OrderQty()).Obj.ToString();
            var priceStr = nos.GetField(new Price()).Obj.ToString();

            var quantity = long.Parse(quantityStr);
            var price = decimal.Parse(priceStr);

            var symbol = SymbolVO.Create(symbolStr);
            var isBuy = sideChar == FixOrderRouting.SharedKernel.Constants.FixConstants.Side.Buy.ToString();
            var side = SideVO.Create(isBuy ? BusinessConstants.Sides.Buy : BusinessConstants.Sides.Sell);
            var qty = QuantityVO.Create(quantity);
            var prc = PriceVO.Create(price);

            var canAccept = _exposureCalculator.CanAcceptOrderAsync(symbol, side, qty, prc).GetAwaiter().GetResult();

            OrderAccumulator.Domain.Aggregates.OrderExecution execution;
            if (canAccept)
            {
                execution = OrderAccumulator.Domain.Aggregates.OrderExecution.CreateAccepted(clOrdId, symbol, side, qty, prc);
                var exposure = _exposureCalculator.GetExposureAsync(symbolStr).GetAwaiter().GetResult();
                _logger.LogInformation("Order accepted: ClOrdID={ClOrdID}, Symbol={Symbol}, Exposure={Exposure}",
                    clOrdId, symbolStr, exposure);
            }
            else
            {
                var rejectionReason = $"Financial exposure would exceed limit of R$ {BusinessConstants.Orders.ExposureLimit:N0}";
                execution = OrderAccumulator.Domain.Aggregates.OrderExecution.CreateRejected(clOrdId, symbol, side, qty, prc, rejectionReason);
                _logger.LogWarning("Order rejected: ClOrdID={ClOrdID}, Reason={Reason}", clOrdId, rejectionReason);
            }

            _repository.AddAsync(execution).GetAwaiter().GetResult();
            SendExecutionReport(nos, execution, sessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NewOrderSingle");
            throw;
        }
    }

    private void SendExecutionReport(NewOrderSingle nos, OrderAccumulator.Domain.Aggregates.OrderExecution execution, SessionID sessionID)
    {
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
                execReport.Set(new ExecType(FixOrderRouting.SharedKernel.Constants.FixConstants.ExecType.New[0]));
                execReport.Set(new OrdStatus(FixOrderRouting.SharedKernel.Constants.FixConstants.OrdStatus.Filled));
                execReport.Set(new LeavesQty(0));
                execReport.Set(new CumQty((int)execution.Quantity.Value));
                execReport.Set(new AvgPx(execution.Price.Value));
                execReport.Set(new Text("Order accepted - Exposure within limit"));
            }
            else
            {
                execReport.Set(new ExecType(FixOrderRouting.SharedKernel.Constants.FixConstants.ExecType.Rejected[0]));
                execReport.Set(new OrdStatus(FixOrderRouting.SharedKernel.Constants.FixConstants.OrdStatus.Rejected));
                execReport.Set(new Text(execution.RejectionReason ?? "Order rejected"));
            }

            Session.SendToTarget(execReport, sessionID);
            _logger.LogInformation("ExecutionReport sent: ClOrdID={ClOrdID}, Status={Status}",
                execution.ClOrdId, execution.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ExecutionReport for ClOrdID={ClOrdID}", execution.ClOrdId);
        }
    }
}
