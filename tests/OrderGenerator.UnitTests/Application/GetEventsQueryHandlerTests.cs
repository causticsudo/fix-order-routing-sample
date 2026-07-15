using AwesomeAssertions;
using Moq;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.GetEvents;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.Aggregates.Enumerators;
using Xunit;

namespace OrderGenerator.UnitTests.Application;

public class GetEventsQueryHandlerTests
{
    private readonly Mock<IOrderEventRepository> _repositoryMock;
    private readonly GetEventsQueryHandler _sut;

    public GetEventsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IOrderEventRepository>();
        _sut = new GetEventsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsMappedPagedResponse()
    {
        var orderId = Guid.NewGuid();
        var events = new List<OrderEvent>
        {
            OrderEvent.Create(orderId, orderId.ToString(), OrderEventType.Created),
            OrderEvent.Create(orderId, orderId.ToString(), OrderEventType.Submitted)
        };

        _repositoryMock.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((events, events.Count));

        var response = await _sut.Handle(new GetEventsQuery(1, 20, null), CancellationToken.None);

        response.Items.Should().HaveCount(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
        response.TotalCount.Should().Be(2);
        response.Items[0].EventType.Should().Be("Created");
    }

    [Fact]
    public async Task Handle_ComputesTotalPagesFromTotalCountAndPageSize()
    {
        _repositoryMock.Setup(r => r.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OrderEvent>(), 25));

        var response = await _sut.Handle(new GetEventsQuery(1, 10, null), CancellationToken.None);

        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithOrderIdFilter_PassesOrderIdToRepository()
    {
        var orderId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetPagedAsync(1, 20, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OrderEvent>(), 0));

        await _sut.Handle(new GetEventsQuery(1, 20, orderId), CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(1, 20, orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroPage_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.Handle(new GetEventsQuery(0, 20, null), CancellationToken.None));

        exception.Message.Should().Contain("Page must be greater than 0");
    }

    [Fact]
    public async Task Handle_WithPageSizeGreaterThan100_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.Handle(new GetEventsQuery(1, 101, null), CancellationToken.None));

        exception.Message.Should().Contain("PageSize must be less than or equal to 100");
    }
}
