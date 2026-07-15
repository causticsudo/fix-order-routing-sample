using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Infra.Caching;
using OrderAccumulator.Infra.Persistence;
using OrderAccumulator.Worker.FIX;
using Serilog;
using StackExchange.Redis;

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext();
});

builder.ConfigureServices((context, services) =>
{
    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

    services.AddDbContext<OrderAccumulatorDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IOrderExecutionRepository, OrderExecutionRepository>();
    services.AddScoped<ExposureCalculator>();

    var redisHost = context.Configuration["Redis:Host"] ?? "localhost";
    var redisPort = context.Configuration["Redis:Port"] ?? "6379";
    services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect($"{redisHost}:{redisPort}"));
    services.AddSingleton<IExposureCache, RedisExposureCache>();

    services.AddSingleton<FixOrderListener>();
    services.AddHostedService<FixAcceptorService>();
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderAccumulatorDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
