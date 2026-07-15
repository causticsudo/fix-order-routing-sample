using OrderGenerator.Domain.Aggregates.Enumerators;
using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Domain.Aggregates;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Symbol Symbol { get; private set; } = null!;
    public OrderSide Side { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public Price Price { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Order(
        Guid id,
        Symbol symbol,
        OrderSide side,
        Quantity quantity,
        Price price,
        OrderStatus status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        Price = price;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Order> Create(
        Symbol symbol,
        OrderSide side,
        Quantity quantity,
        Price price)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var order = new Order(id, symbol, side, quantity, price, OrderStatus.Created, now, now);

        return ResultExtensions.Success(order);
    }

    public void MarkAsSubmitted()
    {
        Status = OrderStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsAccepted()
    {
        Status = OrderStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRejected(string reason)
    {
        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
