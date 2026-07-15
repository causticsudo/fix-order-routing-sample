using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Aggregates;

namespace OrderAccumulator.Infra.Persistence;

public class InMemoryOrderExecutionRepository : IOrderExecutionRepository
{
    private readonly List<OrderExecution> _orders = new();
    private readonly ILogger<InMemoryOrderExecutionRepository> _logger;

    public InMemoryOrderExecutionRepository(ILogger<InMemoryOrderExecutionRepository> logger)
    {
        _logger = logger;
    }

    public async Task AddAsync(OrderExecution execution, CancellationToken cancellationToken = default)
    {
        _orders.Add(execution);
        _logger.LogInformation("Order persisted to in-memory store: {ClOrdId}", execution.ClOrdId);
        await Task.CompletedTask;
    }

    public async Task<OrderExecution?> GetByClOrdIdAsync(string clOrdId, CancellationToken cancellationToken = default)
    {
        var order = _orders.FirstOrDefault(o => o.ClOrdId == clOrdId);
        await Task.CompletedTask;
        return order;
    }

    public async Task<IEnumerable<OrderExecution>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var orders = _orders.Where(o => o.Symbol.Value == symbol).ToList();
        await Task.CompletedTask;
        return orders;
    }

    public async Task<IEnumerable<OrderExecution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _orders.ToList();
    }
}
