using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Application.Abstractions;

public interface IOrderCache
{
    void Set(Order order, TimeSpan? expiration = null);
    bool TryGet(Guid orderId, out Order? order);
    void Remove(Guid orderId);
}
