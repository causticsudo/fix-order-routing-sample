using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.Services;
using OrderAccumulator.Infra.Persistence;
using OrderAccumulator.Worker.FIX;
using Serilog;

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog((context, logger) =>
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
