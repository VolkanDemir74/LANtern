using LANtern.Host.Settings;

namespace LANtern.Host.Streaming;

public sealed class StreamRecoveryService(
    StreamCoordinator stream,
    LanternSettingsService settings,
    ILogger<StreamRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = TimeSpan.FromSeconds(2);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(retryDelay, stoppingToken);
                var preferences = await settings.GetAsync();
                if (!preferences.AutoRecoverStream || !stream.ShouldBeRunning || stream.IsRunning)
                {
                    retryDelay = TimeSpan.FromSeconds(2);
                    continue;
                }

                var result = await stream.TryRecoverAsync(stoppingToken);
                if (result is null) continue;
                if (result.Success)
                {
                    retryDelay = TimeSpan.FromSeconds(2);
                    logger.LogInformation("Yayın otomatik olarak yeniden başlatıldı.");
                }
                else
                {
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 15));
                    logger.LogWarning("Yayın kurtarma denemesi başarısız: {Message}. Sonraki deneme {Delay} saniye sonra.",
                        result.Message, retryDelay.TotalSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 15));
                logger.LogWarning(exception, "Yayın otomatik olarak yeniden başlatılamadı.");
            }
        }
    }
}
