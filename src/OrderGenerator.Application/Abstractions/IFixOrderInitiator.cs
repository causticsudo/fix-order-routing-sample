using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Application.Abstractions;

public interface IFixOrderInitiator
{
    Task SendNewOrderSingleAsync(Order order, CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}
