using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Domain.Aggregates;

public record OrderCreatedEvent(
    Guid OrderId,
    Symbol Symbol,
    OrderSide Side,
    Quantity Quantity,
    Price Price,
    DateTime CreatedAt);
