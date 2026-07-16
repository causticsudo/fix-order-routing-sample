using MediatR;
using OrderGenerator.Application.Common;

namespace OrderGenerator.Application.Features.Orders.GetEvents;

public sealed record GetEventsQuery(int Page, int PageSize, Guid? OrderId) : IRequest<PagedResponse<OrderEventDto>>;
