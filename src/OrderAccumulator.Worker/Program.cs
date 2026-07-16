using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Infra.Caching;
using OrderAccumulator.Infra.Persistence;
using OrderAccumulator.Worker.FIX;
using FixOrderRouting.SharedKernel.Diagnostics;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using StackExchange.Redis;

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithOpenTelemetryTraceId()
        .Enrich.WithOpenTelemetrySpanId();
});

builder.ConfigureServices((context, services) =>
{
    var serviceName = context.Configuration["Service:Name"] ?? "OrderAccumulator";

    services.AddOpenTelemetry()
        .WithTracing(tracerProvider => tracerProvider
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddSqlClientInstrumentation()
            .AddSource(FixActivitySource.Instance.Name)
            .AddConsoleExporter());

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

    services.AddHealthChecks()
        .AddNpgSql(
            connectionString,
            name: "PostgreSQL",
            tags: new[] { "db", "sql" })
        .AddRedis(
            $"{redisHost}:{redisPort}",
            name: "Redis",
            tags: new[] { "cache" });

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
