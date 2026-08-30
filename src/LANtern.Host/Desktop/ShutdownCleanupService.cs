using LANtern.Host.Streaming;
using LANtern.Host.VirtualDisplay;

namespace LANtern.Host.Desktop;

public sealed class ShutdownCleanupService : IHostedService
{
    private readonly StreamCoordinator _stream;
    private readonly VirtualDisplayManager _virtualDisplay;
    private readonly MediaMtxService _mediaMtx;
    private readonly ChildProcessJob _job;
    private readonly ILogger<ShutdownCleanupService> _logger;

    public ShutdownCleanupService(StreamCoordinator stream, VirtualDisplayManager virtualDisplay,
        MediaMtxService mediaMtx, ChildProcessJob job, ILogger<ShutdownCleanupService> logger)
        => (_stream, _virtualDisplay, _mediaMtx, _job, _logger) =
            (stream, virtualDisplay, mediaMtx, job, logger);

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await RunCleanupAsync("yayın", () => _stream.StopAsync());
        await RunCleanupAsync("sanal monitör", () => _virtualDisplay.StopAsync());
        await RunCleanupAsync("WebRTC geçidi", () => _mediaMtx.StopAsync());
        _job.Dispose();
    }

    private async Task RunCleanupAsync(string component, Func<Task> cleanup)
    {
        try
        {
            await cleanup().WaitAsync(TimeSpan.FromSeconds(8));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Kapanış sırasında {Component} zaman aşımına uğradı.", component);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kapanış sırasında {Component} temizlenemedi.", component);
        }
    }
}
