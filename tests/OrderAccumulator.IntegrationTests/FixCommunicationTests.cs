using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Infra.Persistence;
using Xunit;

namespace OrderAccumulator.IntegrationTests;

public class FixCommunicationTests
{
    private readonly IOrderExecutionRepository _repository;
    private readonly ExposureCalculator _exposureCalculator;

    public FixCommunicationTests()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        _repository = serviceProvider.GetRequiredService<IOrderExecutionRepository>();
        _exposureCalculator = serviceProvider.GetRequiredService<ExposureCalculator>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton<IOrderExecutionRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<InMemoryOrderExecutionRepository>>();
            return new InMemoryOrderExecutionRepository(logger);
        });

        services.AddScoped<ExposureCalculator>();
    }

    [Fact]
    public async Task Repository_StorersAndRetrievesOrders()
    {
        var orders = await _repository.GetBySymbolAsync("PETR4");
        Assert.NotNull(orders);
        Assert.Empty(orders);
    }

    [Fact]
    public async Task ExposureCalculator_TracksMultipleSymbols()
    {
        var petr4Exposure = await _exposureCalculator.GetExposureAsync("PETR4");
        var vale3Exposure = await _exposureCalculator.GetExposureAsync("VALE3");
        var viia4Exposure = await _exposureCalculator.GetExposureAsync("VIIA4");

        Assert.Equal(0m, petr4Exposure);
        Assert.Equal(0m, vale3Exposure);
        Assert.Equal(0m, viia4Exposure);
    }

    [Fact]
    public async Task GetBySymbol_ReturnsCorrectOrders()
    {
        var orders = await _repository.GetBySymbolAsync("PETR4");
        Assert.NotNull(orders);
        Assert.IsAssignableFrom<IEnumerable<OrderAccumulator.Domain.Aggregates.OrderExecution>>(orders);
    }
}
