#if WINDOWS
using Squirrel;
#endif
using DbcServer.Core.Configuration;

namespace DbcServer.Api.Services;

public class UpdateService : IHostedService, IDisposable
{
    private readonly AppConfiguration _config;
    private readonly ILogger<UpdateService> _logger;
    private Timer? _timer;
#if WINDOWS
    private UpdateManager? _updateManager;
#else
    private object? _updateManager;
#endif

    public UpdateService(AppConfiguration config, ILogger<UpdateService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.UpdateSettings.EnableAutoUpdate || string.IsNullOrEmpty(_config.UpdateSettings.UpdateUrl))
        {
            _logger.LogInformation("Auto-update is disabled or update URL not configured");
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogInformation("Auto-update is only available on Windows");
            return;
        }

        try
        {
#if WINDOWS
            _updateManager = await UpdateManager.GitHubUpdateManager(_config.UpdateSettings.UpdateUrl);
#endif

            // Check for updates immediately on startup
            await CheckForUpdates();

            // Set up timer to check for updates periodically
            var checkInterval = TimeSpan.FromMinutes(_config.UpdateSettings.CheckIntervalMinutes);
            _timer = new Timer(async _ => await CheckForUpdates(), null,
                checkInterval, checkInterval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize update manager");
        }
    }

    private async Task CheckForUpdates()
    {
#if WINDOWS
        if (_updateManager == null) return;

        try
        {
            _logger.LogInformation("Checking for updates...");
            
            var updateInfo = await _updateManager.CheckForUpdate();
            
            if (updateInfo.ReleasesToApply.Any())
            {
                _logger.LogInformation($"Update available: {updateInfo.FutureReleaseEntry.Version}");
                
                // Download and apply updates
                await _updateManager.UpdateApp();
                
                _logger.LogInformation("Update downloaded and will be applied on next restart");
            }
            else
            {
                _logger.LogInformation("No updates available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
        }
#else
        await Task.CompletedTask;
        _logger.LogInformation("Auto-update is only available on Windows");
#endif
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
#if WINDOWS
        _updateManager?.Dispose();
#endif
    }
}