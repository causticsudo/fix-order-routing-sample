using FixOrderRouting.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Aggregates;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Domain.ValueObjects;
using OrderAccumulator.Infra.Persistence;
using Xunit;

namespace OrderAccumulator.IntegrationTests;

public class ExposureCalculatorTests
{
    private readonly ExposureCalculator _sut;
    private readonly IOrderExecutionRepository _repository;

    public ExposureCalculatorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IOrderExecutionRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<InMemoryOrderExecutionRepository>>();
            return new InMemoryOrderExecutionRepository(logger);
        });
        services.AddScoped<ExposureCalculator>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        _sut = scope.ServiceProvider.GetRequiredService<ExposureCalculator>();
        _repository = scope.ServiceProvider.GetRequiredService<IOrderExecutionRepository>();
    }

    [Fact]
    public async Task BuyOrder_IncreasesExposure()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);
        var qty = Quantity.Create(1000);
        var price = Price.Create(25.50m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.True(canAccept);
    }

    [Fact]
    public async Task SellOrder_DecreasesExposure()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create(BusinessConstants.Sides.Sell);
        var qty = Quantity.Create(1000);
        var price = Price.Create(25.50m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.True(canAccept);
    }

    [Fact]
    public async Task GetExposure_ReturnsZeroForNoOrders()
    {
        var exposure = await _sut.GetExposureAsync("VIIA4");
        Assert.Equal(0m, exposure);
    }

    [Fact]
    public async Task AcceptBuyOrder_AtPositiveLimit()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);
        var qty = Quantity.Create(100_000);
        var price = Price.Create(1000m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.True(canAccept);
    }

    [Fact]
    public async Task RejectBuyOrder_BeyondPositiveLimit()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);
        var qty = Quantity.Create(100_001);
        var price = Price.Create(1000m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.False(canAccept);
    }

    [Fact]
    public async Task AcceptSellOrder_AtNegativeLimit()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create(BusinessConstants.Sides.Sell);
        var qty = Quantity.Create(100_000);
        var price = Price.Create(1000m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.True(canAccept);
    }

    [Fact]
    public async Task RejectSellOrder_BeyondNegativeLimit()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create(BusinessConstants.Sides.Sell);
        var qty = Quantity.Create(100_001);
        var price = Price.Create(1000m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);
        Assert.False(canAccept);
    }

    [Fact]
    public async Task AllowNegativeExposure_WithinRange()
    {
        var symbol = Symbol.Create("PETR4");
        var clOrdId = "ORDER_001";

        var buy50m = OrderExecution.CreateAccepted(
            clOrdId: clOrdId,
            symbol: Symbol.Create("PETR4"),
            side: OrderSide.Create(BusinessConstants.Sides.Buy),
            quantity: Quantity.Create(50_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(buy50m);

        var canAcceptSell = await _sut.CanAcceptOrderAsync(
            Symbol.Create("PETR4"),
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(60_000),
            Price.Create(1000m)
        );

        Assert.True(canAcceptSell);
    }

    [Fact]
    public async Task RejectSellOrder_ExposureWouldBeBelowNegativeLimit()
    {
        var clOrdId = "ORDER_002";

        var sell90m = OrderExecution.CreateAccepted(
            clOrdId: clOrdId,
            symbol: Symbol.Create("VALE3"),
            side: OrderSide.Create(BusinessConstants.Sides.Sell),
            quantity: Quantity.Create(90_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(sell90m);

        var currentExposure = await _sut.GetExposureAsync("VALE3");
        Assert.Equal(-90_000_000m, currentExposure);

        var canAcceptMoreSell = await _sut.CanAcceptOrderAsync(
            Symbol.Create("VALE3"),
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(15_000),
            Price.Create(1000m)
        );

        Assert.False(canAcceptMoreSell);
    }

    [Fact]
    public async Task AcceptSellOrder_ButRemainNegative_WithinRange()
    {
        var clOrdId = "ORDER_003";

        var sell90m = OrderExecution.CreateAccepted(
            clOrdId: clOrdId,
            symbol: Symbol.Create("VIIA4"),
            side: OrderSide.Create(BusinessConstants.Sides.Sell),
            quantity: Quantity.Create(90_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(sell90m);

        var currentExposure = await _sut.GetExposureAsync("VIIA4");
        Assert.Equal(-90_000_000m, currentExposure);

        var canAcceptMoreSell = await _sut.CanAcceptOrderAsync(
            Symbol.Create("VIIA4"),
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(5_000),
            Price.Create(1000m)
        );

        Assert.True(canAcceptMoreSell);
    }

    [Fact]
    public async Task RejectBuyOrder_ExposureWouldBeAbovePositiveLimit()
    {
        var clOrdId = "ORDER_004";

        var buy90m = OrderExecution.CreateAccepted(
            clOrdId: clOrdId,
            symbol: Symbol.Create("PETR4"),
            side: OrderSide.Create(BusinessConstants.Sides.Buy),
            quantity: Quantity.Create(90_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(buy90m);

        var currentExposure = await _sut.GetExposureAsync("PETR4");
        Assert.Equal(90_000_000m, currentExposure);

        var canAcceptMoreBuy = await _sut.CanAcceptOrderAsync(
            Symbol.Create("PETR4"),
            OrderSide.Create(BusinessConstants.Sides.Buy),
            Quantity.Create(15_000),
            Price.Create(1000m)
        );

        Assert.False(canAcceptMoreBuy);
    }

    [Fact]
    public async Task AcceptBuyOrder_ButRemainPositive_WithinRange()
    {
        var clOrdId = "ORDER_005";

        var buy90m = OrderExecution.CreateAccepted(
            clOrdId: clOrdId,
            symbol: Symbol.Create("PETR4"),
            side: OrderSide.Create(BusinessConstants.Sides.Buy),
            quantity: Quantity.Create(90_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(buy90m);

        var currentExposure = await _sut.GetExposureAsync("PETR4");
        Assert.Equal(90_000_000m, currentExposure);

        var canAcceptMoreBuy = await _sut.CanAcceptOrderAsync(
            Symbol.Create("PETR4"),
            OrderSide.Create(BusinessConstants.Sides.Buy),
            Quantity.Create(5_000),
            Price.Create(1000m)
        );

        Assert.True(canAcceptMoreBuy);
    }

    [Fact]
    public async Task ComplexScenario_MixedBuySellSequence()
    {
        var symbol = Symbol.Create("PETR4");

        var order1 = OrderExecution.CreateAccepted(
            clOrdId: "ORDER_001",
            symbol: symbol,
            side: OrderSide.Create(BusinessConstants.Sides.Buy),
            quantity: Quantity.Create(40_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(order1);
        var exp1 = await _sut.GetExposureAsync("PETR4");
        Assert.Equal(40_000_000m, exp1);

        var order2 = OrderExecution.CreateAccepted(
            clOrdId: "ORDER_002",
            symbol: symbol,
            side: OrderSide.Create(BusinessConstants.Sides.Sell),
            quantity: Quantity.Create(20_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(order2);
        var exp2 = await _sut.GetExposureAsync("PETR4");
        Assert.Equal(20_000_000m, exp2);

        var can3 = await _sut.CanAcceptOrderAsync(
            symbol,
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(60_000),
            Price.Create(1000m)
        );
        Assert.True(can3);
        var order3 = OrderExecution.CreateAccepted(
            clOrdId: "ORDER_003",
            symbol: symbol,
            side: OrderSide.Create(BusinessConstants.Sides.Sell),
            quantity: Quantity.Create(60_000),
            price: Price.Create(1000m)
        );
        await _repository.AddAsync(order3);
        var exp3 = await _sut.GetExposureAsync("PETR4");
        Assert.Equal(-40_000_000m, exp3);

        var can4 = await _sut.CanAcceptOrderAsync(
            symbol,
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(60_000),
            Price.Create(1000m)
        );
        Assert.True(can4);

        var can5 = await _sut.CanAcceptOrderAsync(
            symbol,
            OrderSide.Create(BusinessConstants.Sides.Sell),
            Quantity.Create(1),
            Price.Create(1000m)
        );
        Assert.False(can5);
    }
}
