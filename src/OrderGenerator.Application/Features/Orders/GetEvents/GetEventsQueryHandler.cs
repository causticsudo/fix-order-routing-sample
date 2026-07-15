using MediatR;
using OrderGenerator.Application.Common;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Application.Features.Orders.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResponse<OrderEventResponse>>
{
    private readonly IOrderEventRepository _repository;

    public GetEventsQueryHandler(IOrderEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<OrderEventResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var validation = await new GetEventsQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(e => e.ErrorMessage));
        }

        var (items, totalCount) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.OrderId, cancellationToken);

        var responseItems = items.Select(MapToResponse).ToList();

        return new PagedResponse<OrderEventResponse>(responseItems, request.Page, request.PageSize, totalCount);
    }

    private static OrderEventResponse MapToResponse(OrderEvent orderEvent) => new(
        orderEvent.Id,
        orderEvent.OrderId,
        orderEvent.CorrelationKey,
        orderEvent.EventType.ToString(),
        orderEvent.Details,
        orderEvent.OccurredAt);
}
