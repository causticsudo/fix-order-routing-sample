using AwesomeAssertions;
using Moq;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using Xunit;

namespace OrderGenerator.UnitTests.Application;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly Mock<IOrderCache> _cacheMock;
    private readonly Mock<IFixOrderInitiator> _fixInitiatorMock;
    private readonly CreateOrderCommandHandler _sut;

    public CreateOrderCommandHandlerTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _cacheMock = new Mock<IOrderCache>();
        _fixInitiatorMock = new Mock<IFixOrderInitiator>();
        _sut = new CreateOrderCommandHandler(_repositoryMock.Object, _cacheMock.Object, _fixInitiatorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesOrderSuccessfully()
    {
        _fixInitiatorMock.Setup(f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand("PETR4", "BUY", 100, 20.50m);

        var response = await _sut.Handle(command, CancellationToken.None);

        response.Should().NotBeNull();
        response.Symbol.Should().Be("PETR4");
        response.Side.Should().Be("BUY");
        response.Quantity.Should().Be(100L);
        response.Price.Should().Be(20.50m);
        response.Status.Should().Be("Submitted");
        response.OrderId.Should().NotBe(Guid.Empty);
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsOrderToRepository()
    {
        _fixInitiatorMock.Setup(f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand("VALE3", "SELL", 50, 15.75m);

        await _sut.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CachesOrder()
    {
        _fixInitiatorMock.Setup(f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand("VIIA4", "BUY", 25, 99.99m);

        await _sut.Handle(command, CancellationToken.None);

        _cacheMock.Verify(c => c.Set(It.IsAny<Order>(), null), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithInvalidSymbol_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("INVALID", "BUY", 100, 20.50m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Symbol must be one of: PETR4, VALE3, VIIA4");
    }

    [Fact]
    public async Task Handle_WithInvalidSide_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "INVALID", 100, 20.50m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Side must be either BUY or SELL");
    }

    [Fact]
    public async Task Handle_WithZeroQuantity_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 0, 20.50m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Quantity must be greater than 0");
    }

    [Fact]
    public async Task Handle_WithQuantityGreaterThan100k_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100_000, 20.50m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Quantity must be less than 100,000");
    }

    [Fact]
    public async Task Handle_WithZeroPrice_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 0);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Price must be greater than 0");
    }

    [Fact]
    public async Task Handle_WithPriceGreaterThan1000_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 1_000);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Price must be less than 1,000");
    }

    [Fact]
    public async Task Handle_WithPriceNotMultipleOf001_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 20.555m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Price must be a multiple of 0.01");
    }

    [Theory]
    [InlineData("petr4")]
    [InlineData("Petr4")]
    public async Task Handle_WithDifferentSymbolCases_NormalizesToUpperCase(string symbol)
    {
        var command = new CreateOrderCommand(symbol, "BUY", 100, 20.50m);

        var response = await _sut.Handle(command, CancellationToken.None);

        response.Symbol.Should().Be("PETR4");
    }

    [Theory]
    [InlineData("buy")]
    [InlineData("Buy")]
    public async Task Handle_WithDifferentSideCases_NormalizesToUpperCase(string side)
    {
        var command = new CreateOrderCommand("PETR4", side, 100, 20.50m);

        var response = await _sut.Handle(command, CancellationToken.None);

        response.Side.Should().Be("BUY");
    }
}
