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
    // Add services here
});

var host = builder.Build();
await host.RunAsync();
