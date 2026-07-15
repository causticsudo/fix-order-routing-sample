using System.Globalization;
using OrderAccumulator.Domain.Abstractions;
using StackExchange.Redis;

namespace OrderAccumulator.Infra.Caching;

public class RedisExposureCache : IExposureCache
{
    private const string HashKey = "exposures";
    private readonly IConnectionMultiplexer _redis;

    public RedisExposureCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SetExposureAsync(string symbol, decimal exposure, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.HashSetAsync(HashKey, symbol, exposure.ToString(CultureInfo.InvariantCulture));
    }
}
