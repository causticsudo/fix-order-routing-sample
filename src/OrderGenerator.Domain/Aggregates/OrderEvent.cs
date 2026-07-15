using OrderGenerator.Domain.Aggregates.Enumerators;

namespace OrderGenerator.Domain.Aggregates;

public sealed class OrderEvent
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string CorrelationKey { get; private set; } = null!;
    public OrderEventType EventType { get; private set; }
    public string? Details { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private OrderEvent() { }

    public static OrderEvent Create(Guid orderId, string correlationKey, OrderEventType eventType, string? details = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CorrelationKey = correlationKey,
            EventType = eventType,
            Details = details,
            OccurredAt = DateTime.UtcNow
        };
}
