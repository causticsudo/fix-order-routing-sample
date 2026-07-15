using MediatR;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Domain.Abstractions;

namespace OrderGenerator.Application.Features.Orders.GetOrder;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, CreateOrderResponse>
{
    private readonly IOrderRepository _repository;
    private readonly IOrderCache _cache;

    public GetOrderQueryHandler(IOrderRepository repository, IOrderCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<CreateOrderResponse> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(request.OrderId, out var cachedOrder) && cachedOrder is not null)
            return MapToResponse(cachedOrder);

        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            throw new OrderNotFoundException($"Order with ID {request.OrderId} not found");

        _cache.Set(order);

        return MapToResponse(order);
    }

    private static CreateOrderResponse MapToResponse(Domain.Aggregates.Order order) => new(
        order.Id,
        order.Symbol.Value,
        order.Side.Value,
        order.Quantity.Value,
        order.Price.Value,
        order.Status.ToString(),
        order.CreatedAt);
}
