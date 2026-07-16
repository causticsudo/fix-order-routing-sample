using MediatR;
using OrderGenerator.Application.Common;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Application.Features.Orders.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResponse<OrderEventDto>>
{
    private readonly IOrderEventRepository _repository;

    public GetEventsQueryHandler(IOrderEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<OrderEventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var validation = await new GetEventsQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(e => e.ErrorMessage));
        }

        var (items, totalCount) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.OrderId, cancellationToken);

        var responseItems = items.Select(MapToDto).ToList();

        return new PagedResponse<OrderEventDto>(responseItems, request.Page, request.PageSize, totalCount);
    }

    private static OrderEventDto MapToDto(OrderEvent orderEvent) => new(
        EventId: orderEvent.Id,
        OrderId: orderEvent.OrderId,
        CorrelationKey: orderEvent.CorrelationKey,
        Status: orderEvent.EventType.ToString(),
        Timestamp: orderEvent.OccurredAt,
        Reason: orderEvent.Details);
}
