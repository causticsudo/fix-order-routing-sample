namespace OrderGenerator.Application.Abstractions;

public interface IExposureReader
{
    Task<IReadOnlyDictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}
