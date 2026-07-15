using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Application.Features.Orders.GetEvents;
using OrderGenerator.Application.Features.Orders.GetOrder;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Infra.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderGenerator.IntegrationTests.Controllers;

public class OrdersControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("order_generator_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private OrderGeneratorDbContext _dbContext = null!;
    private CreateOrderCommandHandler _createOrderHandler = null!;
    private GetOrderQueryHandler _getOrderHandler = null!;
    private GetEventsQueryHandler _getEventsHandler = null!;
    private Mock<IFixOrderInitiator> _fixInitiatorMock = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<OrderGeneratorDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _dbContext = new OrderGeneratorDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var repository = new OrderRepository(_dbContext);
        var eventRepository = new OrderEventRepository(_dbContext);

        _fixInitiatorMock = new Mock<IFixOrderInitiator>();
        _fixInitiatorMock
            .Setup(f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _createOrderHandler = new CreateOrderCommandHandler(repository, _fixInitiatorMock.Object, eventRepository);
        _getOrderHandler = new GetOrderQueryHandler(repository);
        _getEventsHandler = new GetEventsQueryHandler(eventRepository);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ThenGetOrder_ReturnsSamePersistedOrder()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 20.50m);

        var created = await _createOrderHandler.Handle(command, CancellationToken.None);
        var fetched = await _getOrderHandler.Handle(new GetOrderQuery(created.OrderId), CancellationToken.None);

        fetched.OrderId.Should().Be(created.OrderId);
        fetched.Symbol.Should().Be("PETR4");
        fetched.Side.Should().Be("BUY");
        fetched.Quantity.Should().Be(100L);
        fetched.Price.Should().Be(20.50m);
        fetched.Status.Should().Be("Submitted");
        fetched.RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_SendsNewOrderSingleViaFix()
    {
        var command = new CreateOrderCommand("VALE3", "SELL", 50, 15.75m);

        await _createOrderHandler.Handle(command, CancellationToken.None);

        _fixInitiatorMock.Verify(
            f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrder_PersistsCreatedAndSubmittedEvents()
    {
        var command = new CreateOrderCommand("VIIA4", "BUY", 25, 99.99m);

        var created = await _createOrderHandler.Handle(command, CancellationToken.None);
        var events = await _getEventsHandler.Handle(new GetEventsQuery(1, 10, created.OrderId), CancellationToken.None);

        events.TotalCount.Should().Be(2);
        events.Items.Should().Contain(e => e.EventType == "Created" && e.CorrelationKey == created.OrderId.ToString());
        events.Items.Should().Contain(e => e.EventType == "Submitted");
    }

    [Fact]
    public async Task CreateOrder_WithInvalidSymbol_ThrowsValidationException()
    {
        var command = new CreateOrderCommand("INVALID", "BUY", 100, 20.50m);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _createOrderHandler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Symbol must be one of: PETR4, VALE3, VIIA4");
        _fixInitiatorMock.Verify(
            f => f.SendNewOrderSingleAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrder_WithNonExistentId_ThrowsOrderNotFoundException()
    {
        var orderId = Guid.NewGuid();

        await Assert.ThrowsAsync<OrderNotFoundException>(
            () => _getOrderHandler.Handle(new GetOrderQuery(orderId), CancellationToken.None));
    }

    [Fact]
    public async Task GetEvents_WithoutOrderId_ReturnsAllEventsPaginated()
    {
        await _createOrderHandler.Handle(new CreateOrderCommand("PETR4", "BUY", 10, 10.00m), CancellationToken.None);
        await _createOrderHandler.Handle(new CreateOrderCommand("VALE3", "SELL", 20, 20.00m), CancellationToken.None);

        var page = await _getEventsHandler.Handle(new GetEventsQuery(1, 2, null), CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(4);
    }
}
