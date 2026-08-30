using LANtern.Host.Desktop;
using LANtern.Host.Settings;

namespace LANtern.Host.Updates;

public sealed class StartupUpdateService(UpdateService updates, LanternSettingsService settings, TrayApplication tray, ILogger<StartupUpdateService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2500, stoppingToken);
            var preferences = await settings.GetAsync();
            if (!preferences.StartInTray || !preferences.CheckForUpdates) return;
            var result = await updates.CheckAsync(false, stoppingToken);
            if (result.Available && result.LatestVersion is not null)
                tray.ShowUpdateAvailable(result.LatestVersion);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogWarning(ex, "Başlangıç güncelleme denetimi tamamlanamadı."); }
    }
}
