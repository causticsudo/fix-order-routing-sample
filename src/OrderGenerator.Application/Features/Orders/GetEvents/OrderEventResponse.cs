namespace OrderGenerator.Application.Features.Orders.GetEvents;

public sealed record OrderEventResponse(
    Guid Id,
    Guid OrderId,
    string CorrelationKey,
    string EventType,
    string? Details,
    DateTime OccurredAt);
