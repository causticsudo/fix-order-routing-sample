using MediatR;

namespace OrderGenerator.Application.Features.Orders.CreateOrder;

public record CreateOrderCommand(
    string Symbol,
    string Side,
    long Quantity,
    decimal Price) : IRequest<CreateOrderResponse>;
