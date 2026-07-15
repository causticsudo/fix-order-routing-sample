using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Domain.Abstractions;

public interface IOrderEventRepository
{
    Task AddAsync(OrderEvent orderEvent, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrderEvent> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? orderId = null,
        CancellationToken cancellationToken = default);
}
