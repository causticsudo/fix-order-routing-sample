namespace OrderGenerator.Application.Features.Orders.GetEvents;

public sealed record OrderEventDto(
    Guid EventId,
    Guid OrderId,
    string CorrelationKey,
    string Status,
    DateTime Timestamp,
    string? Reason);
