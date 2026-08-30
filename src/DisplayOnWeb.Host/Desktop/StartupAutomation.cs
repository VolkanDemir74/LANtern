using DisplayOnWeb.Host.Capture;
using DisplayOnWeb.Host.Settings;
using DisplayOnWeb.Host.Streaming;
using DisplayOnWeb.Host.VirtualDisplay;

namespace DisplayOnWeb.Host.Desktop;

public sealed class StartupAutomation : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly LanternSettingsService _settings;
    private readonly VirtualDisplayManager _virtualDisplay;
    private readonly DisplayCatalog _displays;
    private readonly StreamCoordinator _stream;
    private readonly ILogger<StartupAutomation> _logger;

    public StartupAutomation(IHostApplicationLifetime lifetime, LanternSettingsService settings, VirtualDisplayManager virtualDisplay,
        DisplayCatalog displays, StreamCoordinator stream, ILogger<StartupAutomation> logger)
        => (_lifetime, _settings, _virtualDisplay, _displays, _stream, _logger) = (lifetime, settings, virtualDisplay, displays, stream, logger);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() => _ = RunAsync(cancellationToken));
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            var settings = await _settings.GetAsync();
            if (settings.AutoConnectVirtualDisplay && !_virtualDisplay.IsConnected) await _virtualDisplay.StartAsync();
            if (!settings.AutoStartStream) return;

            IReadOnlyList<DisplayInfo> displays = [];
            for (var attempt = 0; attempt < 10 && !ct.IsCancellationRequested; attempt++)
            {
                displays = _displays.GetDisplays();
                if (FindDisplay(displays, settings) is not null) break;
                await Task.Delay(500, ct);
            }
            var selected = FindDisplay(displays, settings) ?? displays.FirstOrDefault();
            if (selected is null) return;
            await _stream.StartAsync(new StartStreamRequest(selected.Index, settings.Width, settings.Height, settings.Fps,
                settings.BitrateKbps, settings.Encoder, settings.ScalingMode, settings.CaptureCursor), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Otomatik başlangıç işlemleri tamamlanamadı"); }
    }

    private static DisplayInfo? FindDisplay(IReadOnlyList<DisplayInfo> displays, LanternSettings settings) =>
        displays.FirstOrDefault(d => d.Name.Equals(settings.PreferredDisplayName, StringComparison.OrdinalIgnoreCase)) ??
        (settings.AutoConnectVirtualDisplay ? displays.LastOrDefault(d => !d.Primary && d.Width == settings.Width && d.Height == settings.Height) : null);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
