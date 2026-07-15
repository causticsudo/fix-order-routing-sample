namespace OrderGenerator.Application.Features.Exposures.GetExposures;

public sealed record ExposureResponse(string Symbol, decimal Exposure);
