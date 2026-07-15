using Microsoft.Extensions.Caching.Memory;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Infra.Persistence;

public class OrderCache(IMemoryCache cache) : IOrderCache
{
    private const string KeyPrefix = "order:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public void Set(Order order, TimeSpan? expiration = null)
    {
        var key = $"{KeyPrefix}{order.Id}";
        cache.Set(key, order, expiration ?? DefaultExpiration);
    }

    public bool TryGet(Guid orderId, out Order? order)
    {
        var key = $"{KeyPrefix}{orderId}";
        return cache.TryGetValue(key, out order);
    }

    public void Remove(Guid orderId)
    {
        var key = $"{KeyPrefix}{orderId}";
        cache.Remove(key);
    }
}
