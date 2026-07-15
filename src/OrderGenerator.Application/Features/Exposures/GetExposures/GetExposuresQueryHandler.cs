using MediatR;
using OrderGenerator.Application.Abstractions;

namespace OrderGenerator.Application.Features.Exposures.GetExposures;

public class GetExposuresQueryHandler : IRequestHandler<GetExposuresQuery, IReadOnlyList<ExposureResponse>>
{
    private readonly IExposureReader _reader;

    public GetExposuresQueryHandler(IExposureReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<ExposureResponse>> Handle(GetExposuresQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            var exposure = await _reader.GetBySymbolAsync(request.Symbol, cancellationToken);
            return new List<ExposureResponse> { new(request.Symbol, exposure) };
        }

        var all = await _reader.GetAllAsync(cancellationToken);
        return all.Select(kvp => new ExposureResponse(kvp.Key, kvp.Value)).ToList();
    }
}
