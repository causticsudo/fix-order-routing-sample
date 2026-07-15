using FixOrderRouting.SharedKernel.Enums;
using OrderAccumulator.Domain.ValueObjects;

namespace OrderAccumulator.Domain.Aggregates;

public class OrderExecution
{
    public Guid Id { get; private set; }
    public string ClOrdId { get; private set; }
    public Symbol Symbol { get; private set; }
    public OrderSide Side { get; private set; }
    public Quantity Quantity { get; private set; }
    public Price Price { get; private set; }
    public OrderExecutionStatus Status { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private OrderExecution() { }

    public static OrderExecution CreateAccepted(
        string clOrdId,
        Symbol symbol,
        OrderSide side,
        Quantity quantity,
        Price price)
    {
        return new OrderExecution
        {
            Id = Guid.NewGuid(),
            ClOrdId = clOrdId,
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = price,
            Status = OrderExecutionStatus.Accepted,
            ExecutedAt = DateTime.UtcNow,
            RejectionReason = null
        };
    }

    public static OrderExecution CreateRejected(
        string clOrdId,
        Symbol symbol,
        OrderSide side,
        Quantity quantity,
        Price price,
        string rejectionReason)
    {
        return new OrderExecution
        {
            Id = Guid.NewGuid(),
            ClOrdId = clOrdId,
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = price,
            Status = OrderExecutionStatus.Rejected,
            ExecutedAt = DateTime.UtcNow,
            RejectionReason = rejectionReason
        };
    }

    public decimal GetExecutionAmount()
    {
        return Price.Value * Quantity.Value;
    }
}
