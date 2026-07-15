namespace OrderAccumulator.Domain.Abstractions;

public interface IExposureCache
{
    Task SetExposureAsync(string symbol, decimal exposure, CancellationToken cancellationToken = default);
}
