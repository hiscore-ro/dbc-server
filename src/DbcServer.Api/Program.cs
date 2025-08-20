using DbcServer.Application.Interfaces;
using DbcServer.Application.Services;
using DbcServer.Core.Interfaces;
using DbcServer.Core.Configuration;
using DbcServer.Infrastructure.Data;
using DbcServer.Infrastructure.Configuration;
using DotNetEnv;

// Load .env file if it exists
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add configuration services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<AppConfiguration>(provider =>
{
    var configService = provider.GetRequiredService<IConfigurationService>();
    return configService.GetConfiguration();
});
// Use Singleton for repository to enable caching across requests
builder.Services.AddSingleton<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockService, StockService>();

// Add auto-update service for Windows
if (OperatingSystem.IsWindows())
{
    builder.Services.AddHostedService<DbcServer.Api.Services.UpdateService>();
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DBC Server API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
