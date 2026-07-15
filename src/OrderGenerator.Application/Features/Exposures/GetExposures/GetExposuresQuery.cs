using MediatR;

namespace OrderGenerator.Application.Features.Exposures.GetExposures;

public sealed record GetExposuresQuery(string? Symbol) : IRequest<IReadOnlyList<ExposureResponse>>;
