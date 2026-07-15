using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Aggregates;
using OrderAccumulator.Domain.ValueObjects;

namespace OrderAccumulator.Infra.Persistence;

public class OrderExecutionRepository : IOrderExecutionRepository
{
    private readonly OrderAccumulatorDbContext _dbContext;

    public OrderExecutionRepository(OrderAccumulatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OrderExecution execution, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderExecutions.AddAsync(execution, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderExecution?> GetByClOrdIdAsync(string clOrdId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderExecutions
            .FirstOrDefaultAsync(e => e.ClOrdId == clOrdId, cancellationToken);
    }

    public async Task<IEnumerable<OrderExecution>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var symbolValue = Symbol.Create(symbol);

        return await _dbContext.OrderExecutions
            .Where(e => e.Symbol == symbolValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OrderExecution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderExecutions.ToListAsync(cancellationToken);
    }
}
