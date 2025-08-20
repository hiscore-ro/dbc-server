using System.Text.Json;
using DbcServer.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DbcServer.Infrastructure.Configuration;

public interface IConfigurationService
{
    AppConfiguration GetConfiguration();
    Task SaveConfigurationAsync(AppConfiguration configuration);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configFilePath;
    private readonly string _exampleConfigPath;
    private AppConfiguration? _cachedConfig;

    public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _exampleConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.example.json");
        
        // In production on Windows, create config.json from example if it doesn't exist
        if (OperatingSystem.IsWindows() && !File.Exists(_configFilePath) && File.Exists(_exampleConfigPath))
        {
            try
            {
                File.Copy(_exampleConfigPath, _configFilePath);
                _logger.LogInformation("Created config.json from config.example.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create config.json from example");
            }
        }
    }

    public AppConfiguration GetConfiguration()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        var config = new AppConfiguration();

        // Try to load from config.json (mainly for Windows production)
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var fileConfig = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (fileConfig != null)
                {
                    config = fileConfig;
                    _logger.LogInformation("Configuration loaded from config.json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading config.json, using defaults");
            }
        }

        // Override with environment variables if present (for development)
        var envDbfPath = Environment.GetEnvironmentVariable("DBF_PATH");
        if (!string.IsNullOrEmpty(envDbfPath))
            config.DbfPath = envDbfPath;

        var envServerUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrEmpty(envServerUrl))
            config.ServerUrl = envServerUrl;

        var envEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envEnvironment))
            config.Environment = envEnvironment;

        // Check appsettings.json through IConfiguration
        var appSettingsDbfPath = _configuration["DbfPath"];
        if (!string.IsNullOrEmpty(appSettingsDbfPath) && string.IsNullOrEmpty(envDbfPath))
            config.DbfPath = appSettingsDbfPath;

        _cachedConfig = config;
        return config;
    }

    public async Task SaveConfigurationAsync(AppConfiguration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configFilePath, json);
            _cachedConfig = configuration;
            _logger.LogInformation("Configuration saved to config.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to config.json");
            throw;
        }
    }
}