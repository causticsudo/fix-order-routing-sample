
using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Domain.Aggregates;

public sealed class Order
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public Symbol Symbol { get; private set; } = null!;
    public OrderSide Side { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public Price Price { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<object> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private Order() { }

    private Order(Guid id, Symbol symbol, OrderSide side, Quantity quantity, Price price, OrderStatus status, DateTime createdAt, DateTime updatedAt)
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

        order._domainEvents.Add(new OrderCreatedEvent(
            order.Id,
            order.Symbol,
            order.Side,
            order.Quantity,
            order.Price,
            order.CreatedAt));

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
        UpdatedAt = DateTime.UtcNow;
    }
}
