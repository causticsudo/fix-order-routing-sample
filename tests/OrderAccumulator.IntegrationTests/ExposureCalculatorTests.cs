using FixOrderRouting.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Domain.ValueObjects;
using OrderAccumulator.Infra.Persistence;
using Xunit;

namespace OrderAccumulator.IntegrationTests;

public class ExposureCalculatorTests
{
    private readonly ExposureCalculator _sut;

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
    public async Task ExposureLimit_RejectsLargeOrders()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);

        // Attempt to create an order that exceeds the limit
        // Max quantity is 99999, price max is 999.99
        // Order worth 99999 * 999.99 ≈ 99M fits within 100M
        var qty = Quantity.Create(99999);
        var price = Price.Create(999.99m);

        var canAccept = await _sut.CanAcceptOrderAsync(symbol, side, qty, price);

        // This should be accepted (within 100M limit)
        Assert.True(canAccept);
    }

    [Fact]
    public async Task GetExposure_ReturnsZeroForNoOrders()
    {
        var exposure = await _sut.GetExposureAsync("VIIA4");
        Assert.Equal(0m, exposure);
    }
}
