using Microsoft.EntityFrameworkCore;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Infra.Persistence;

public class OrderEventRepository : IOrderEventRepository
{
    private readonly OrderGeneratorDbContext _context;

    public OrderEventRepository(OrderGeneratorDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderEvent orderEvent, CancellationToken cancellationToken = default)
    {
        await _context.OrderEvents.AddAsync(orderEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OrderEvent> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? orderId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OrderEvents.AsNoTracking().AsQueryable();

        if (orderId.HasValue)
            query = query.Where(e => e.OrderId == orderId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
