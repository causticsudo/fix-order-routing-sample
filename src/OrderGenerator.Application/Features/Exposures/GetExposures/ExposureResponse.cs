namespace OrderGenerator.Application.Features.Exposures.GetExposures;

public sealed record ExposureResponse(
    string Symbol,
    decimal CurrentExposure,
    decimal LimitMin,
    decimal LimitMax)
{
    public ExposureResponse(string symbol, decimal exposure)
        : this(symbol, exposure, -100_000_000m, 100_000_000m) { }
}
