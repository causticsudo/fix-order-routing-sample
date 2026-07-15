using AwesomeAssertions;
using Moq;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.GetOrder;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.ValueObjects;
using Xunit;

namespace OrderGenerator.UnitTests.Application;

public class GetOrderQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly GetOrderQueryHandler _sut;

    public GetOrderQueryHandlerTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _sut = new GetOrderQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsOrderFromRepository()
    {
        var order = CreateTestOrder();
        var orderId = order.Id;
        var query = new GetOrderQuery(orderId);

        _repositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var response = await _sut.Handle(query, CancellationToken.None);

        response.Should().NotBeNull();
        response.OrderId.Should().Be(orderId);
        _repositoryMock.Verify(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ThrowsOrderNotFoundException()
    {
        var orderId = Guid.NewGuid();
        var query = new GetOrderQuery(orderId);

        _repositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<OrderNotFoundException>(() => _sut.Handle(query, CancellationToken.None));
        exception.Message.Should().Contain($"Order with ID {orderId} not found");
    }

    [Fact]
    public async Task Handle_ReturnsCorrectOrderResponse()
    {
        var order = CreateTestOrder();
        var orderId = order.Id;
        var query = new GetOrderQuery(orderId);

        _repositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var response = await _sut.Handle(query, CancellationToken.None);

        response.OrderId.Should().Be(orderId);
        response.Symbol.Should().Be("PETR4");
        response.Side.Should().Be("BUY");
        response.Quantity.Should().Be(100L);
        response.Price.Should().Be(20.50m);
        response.Status.Should().Be("Created");
    }

    [Fact]
    public async Task Handle_WithRejectedOrder_ReturnsRejectionReasonInResponse()
    {
        var order = CreateTestOrder();
        order.MarkAsRejected("Exposure limit exceeded");
        var query = new GetOrderQuery(order.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var response = await _sut.Handle(query, CancellationToken.None);

        response.RejectionReason.Should().Be("Exposure limit exceeded");
    }

    private static Order CreateTestOrder()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create("BUY");
        var quantity = Quantity.Create(100);
        var price = Price.Create(20.50m);

        var result = Order.Create(symbol, side, quantity, price);
        return (result as ResultSuccess<Order>)!.Value;
    }
}
