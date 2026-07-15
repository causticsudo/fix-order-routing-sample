using System.Globalization;
using OrderGenerator.Application.Abstractions;
using StackExchange.Redis;

namespace OrderGenerator.Infra.Caching;

public class RedisExposureReader : IExposureReader
{
    private const string HashKey = "exposures";
    private readonly IConnectionMultiplexer _redis;

    public RedisExposureReader(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(HashKey);

        return entries.ToDictionary(
            e => e.Name.ToString(),
            e => decimal.Parse(e.Value.ToString(), CultureInfo.InvariantCulture));
    }

    public async Task<decimal> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.HashGetAsync(HashKey, symbol);

        return value.HasValue
            ? decimal.Parse(value.ToString(), CultureInfo.InvariantCulture)
            : 0m;
    }
}
