using AwesomeAssertions;
using Moq;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Features.Exposures.GetExposures;
using Xunit;

namespace OrderGenerator.UnitTests.Application;

public class GetExposuresQueryHandlerTests
{
    private readonly Mock<IExposureReader> _readerMock;
    private readonly GetExposuresQueryHandler _sut;

    public GetExposuresQueryHandlerTests()
    {
        _readerMock = new Mock<IExposureReader>();
        _sut = new GetExposuresQueryHandler(_readerMock.Object);
    }

    [Fact]
    public async Task Handle_WithSymbol_ReturnsSingleExposureFromGetBySymbol()
    {
        _readerMock.Setup(r => r.GetBySymbolAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500.50m);

        var response = await _sut.Handle(new GetExposuresQuery("PETR4"), CancellationToken.None);

        response.Should().HaveCount(1);
        response[0].Symbol.Should().Be("PETR4");
        response[0].Exposure.Should().Be(1500.50m);
        _readerMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutSymbol_ReturnsAllExposuresFromGetAll()
    {
        var all = new Dictionary<string, decimal>
        {
            ["PETR4"] = 1000m,
            ["VALE3"] = -500m
        };
        _readerMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(all);

        var response = await _sut.Handle(new GetExposuresQuery(null), CancellationToken.None);

        response.Should().HaveCount(2);
        response.Should().ContainSingle(e => e.Symbol == "PETR4" && e.Exposure == 1000m);
        response.Should().ContainSingle(e => e.Symbol == "VALE3" && e.Exposure == -500m);
        _readerMock.Verify(r => r.GetBySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoExposuresYet_ReturnsEmptyList()
    {
        _readerMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, decimal>());

        var response = await _sut.Handle(new GetExposuresQuery(null), CancellationToken.None);

        response.Should().BeEmpty();
    }
}
