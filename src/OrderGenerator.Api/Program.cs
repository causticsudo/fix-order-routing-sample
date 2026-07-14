using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext();
});

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/v1/health");

app.Run();
