using OrderAccumulator.Domain.Aggregates;

namespace OrderAccumulator.Domain.Abstractions;

public interface IOrderExecutionRepository
{
    Task AddAsync(OrderExecution execution, CancellationToken cancellationToken = default);
    Task<OrderExecution?> GetByClOrdIdAsync(string clOrdId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderExecution>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderExecution>> GetAllAsync(CancellationToken cancellationToken = default);
}
