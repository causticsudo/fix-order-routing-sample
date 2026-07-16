using MediatR;
using OrderGenerator.Application.Common;
using OrderGenerator.Application.Features.Orders.CreateOrder;

namespace OrderGenerator.Application.Features.Orders.ListOrders;

public sealed record ListOrdersQuery(int Page, int PageSize) : IRequest<PagedResponse<CreateOrderResponse>>;
