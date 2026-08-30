using DisplayOnWeb.Host;
using DisplayOnWeb.Host.Capture;
using DisplayOnWeb.Host.Networking;
using DisplayOnWeb.Host.Streaming;
using DisplayOnWeb.Host.Desktop;
using DisplayOnWeb.Host.VirtualDisplay;
using DisplayOnWeb.Host.Settings;
using QRCoder;

Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(builder.Configuration.GetValue("Server:Port", 5000)));
builder.Services.Configure<StreamOptions>(builder.Configuration.GetSection("Stream"));
builder.Services.AddSingleton<LanAddressService>();
builder.Services.AddSingleton<DisplayCatalog>();
builder.Services.AddSingleton<FfmpegLocator>();
builder.Services.AddSingleton<ChildProcessJob>();
builder.Services.AddSingleton<StreamCoordinator>();
builder.Services.AddSingleton<MediaMtxService>();
builder.Services.AddSingleton<VirtualDisplayManager>();
builder.Services.AddSingleton<LanternSettingsService>();
builder.Services.AddHostedService<TrayApplication>();
builder.Services.AddHostedService<StartupAutomation>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets();

app.MapGet("/api/status", (LanAddressService lan, DisplayCatalog displays, StreamCoordinator stream, VirtualDisplayManager virtualDisplay) => Results.Ok(new
{
    running = stream.IsRunning,
    url = lan.GetDisplayUrl(app.Configuration.GetValue("Server:Port", 5000)),
    addresses = lan.GetPrivateLanAddresses(),
    displays = displays.GetDisplays(),
    selectedDisplay = stream.SelectedDisplay,
    receivedPackets = stream.ReceivedPackets,
    encoder = stream.ActiveEncoder,
    error = stream.LastError,
    virtualDisplayConnected = virtualDisplay.IsConnected
}));
app.MapGet("/api/qr", (LanAddressService lan) =>
{
    var url = lan.GetDisplayUrl(app.Configuration.GetValue("Server:Port", 5000)) + "/";
    using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q);
    using var qr = new PngByteQRCode(data);
    return Results.File(qr.GetGraphic(8), "image/png");
});

app.MapPost("/api/stream/start", async (StartStreamRequest request, StreamCoordinator stream, CancellationToken ct) =>
{
    var result = await stream.StartAsync(request, ct);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});
app.MapPost("/api/stream/stop", async (StreamCoordinator stream) => { await stream.StopAsync(); return Results.Ok(); });
app.MapGet("/api/obs/status", (MediaMtxService mediaMtx) => Results.Ok(mediaMtx.GetStatus()));
app.MapPost("/api/obs/start", async (MediaMtxService mediaMtx) =>
{
    var result = await mediaMtx.StartAsync();
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { message = result.Message });
});
app.MapPost("/api/obs/stop", async (MediaMtxService mediaMtx) => { await mediaMtx.StopAsync(); return Results.Ok(); });
app.MapPost("/api/virtual-display/start", async (HttpContext context, VirtualDisplayManager virtualDisplay, LanternSettingsService settingsService,
    DisplayCatalog displays, StreamCoordinator stream, CancellationToken ct) =>
{
    if (context.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await virtualDisplay.StartAsync();
    var settings = await settingsService.GetAsync();
    if (result.Success && settings.AutoStartWhenMonitorConnect && !stream.IsRunning)
    {
        await Task.Delay(700, ct);
        var available = displays.GetDisplays();
        var selected = available.FirstOrDefault(d => d.Name.Equals(settings.PreferredDisplayName, StringComparison.OrdinalIgnoreCase)) ??
                       available.LastOrDefault(d => !d.Primary && d.Width == settings.Width && d.Height == settings.Height);
        if (selected is not null)
        {
            var started = await stream.StartAsync(new StartStreamRequest(selected.Index, settings.Width, settings.Height, settings.Fps,
                settings.BitrateKbps, settings.Encoder, settings.ScalingMode, settings.CaptureCursor), ct);
            if (!started.Success) return Results.BadRequest(started);
        }
    }
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});
app.MapPost("/api/virtual-display/stop", async (HttpContext context, VirtualDisplayManager virtualDisplay, StreamCoordinator stream) =>
{
    if (context.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (stream.IsRunning) await stream.StopAsync();
    var result = await virtualDisplay.StopAsync();
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});
app.MapGet("/api/settings", async (HttpContext context, LanternSettingsService settings) =>
{
    if (context.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(await settings.GetAsync());
});
app.MapPost("/api/settings", async (HttpContext context, LanternSettings value, LanternSettingsService settings) =>
{
    if (context.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    await settings.SaveAsync(value);
    return Results.Ok(new { message = "Ayarlar kaydedildi." });
});
app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Services.GetRequiredService<StreamCoordinator>().Dispose();
    app.Services.GetRequiredService<MediaMtxService>().Dispose();
    app.Services.GetRequiredService<ChildProcessJob>().Dispose();
});
app.Run();

public partial class Program;
