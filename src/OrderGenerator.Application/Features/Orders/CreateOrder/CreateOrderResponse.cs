namespace OrderGenerator.Application.Features.Orders.CreateOrder;

public sealed record CreateOrderResponse(
    Guid OrderId,
    string Symbol,
    string Side,
    long Quantity,
    decimal Price,
    string Status,
    DateTime CreatedAt,
    string? RejectionReason = null);
