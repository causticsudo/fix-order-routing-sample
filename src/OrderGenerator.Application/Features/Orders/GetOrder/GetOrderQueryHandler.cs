using MediatR;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Domain.Abstractions;

namespace OrderGenerator.Application.Features.Orders.GetOrder;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, CreateOrderResponse>
{
    private readonly IOrderRepository _repository;

    public GetOrderQueryHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateOrderResponse> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException($"Order with ID {request.OrderId} not found");
        }

        return MapToResponse(order);
    }

    private static CreateOrderResponse MapToResponse(Domain.Aggregates.Order order) => new(
        order.Id,
        order.Symbol.Value,
        order.Side.Value,
        order.Quantity.Value,
        order.Price.Value,
        order.Status.ToString(),
        order.CreatedAt,
        order.RejectionReason);
}
