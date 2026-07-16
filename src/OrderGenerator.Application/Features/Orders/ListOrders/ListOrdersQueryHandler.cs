using MediatR;
using OrderGenerator.Application.Common;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Domain.Abstractions;

namespace OrderGenerator.Application.Features.Orders.ListOrders;

public class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, PagedResponse<CreateOrderResponse>>
{
    private readonly IOrderRepository _repository;

    public ListOrdersQueryHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<CreateOrderResponse>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var validation = await new ListOrdersQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(e => e.ErrorMessage));
        }

        var (items, totalCount) = await _repository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

        var responseItems = items.Select(MapToResponse).ToList();

        return new PagedResponse<CreateOrderResponse>(responseItems, request.Page, request.PageSize, totalCount);
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
