using Serilog;

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext();
});

builder.ConfigureServices((_, _) =>
{
    // Add services here
});

var host = builder.Build();
await host.RunAsync();
