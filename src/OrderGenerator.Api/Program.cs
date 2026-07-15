using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Infra.Persistence;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Logging
builder.Host.UseSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext();
});
#endregion

#region Services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
#endregion

#region Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<OrderGeneratorDbContext>(options =>
    options.UseNpgsql(connectionString));
#endregion

#region Validation
//Voltar pra tentar configurar o pipeline behavior no mediatr
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommand).Assembly);
#endregion

#region CQRS
builder.Services.AddMediatR(typeof(CreateOrderCommand).Assembly);
#endregion

#region Repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IOrderCache, OrderCache>();
#endregion

#region Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not found");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderGeneratorDbContext>();
    //todo: por enquanto, não sei se vou usar .sql no composer
    await db.Database.EnsureCreatedAsync();
}

#region Middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Endpoints
app.MapControllers();
app.MapHealthChecks("/api/v1/health");
#endregion

await app.RunAsync();
