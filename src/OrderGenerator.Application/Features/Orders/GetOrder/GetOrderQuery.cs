using MediatR;
using OrderGenerator.Application.Features.Orders.CreateOrder;

namespace OrderGenerator.Application.Features.Orders.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<CreateOrderResponse>;
