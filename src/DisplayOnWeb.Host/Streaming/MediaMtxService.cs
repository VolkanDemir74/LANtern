using System.Diagnostics;
using System.Security.Cryptography;
using DisplayOnWeb.Host.Networking;

namespace DisplayOnWeb.Host.Streaming;

public sealed class MediaMtxService : IDisposable
{
    private readonly LanAddressService _lan;
    private readonly ILogger<MediaMtxService> _logger;
    private readonly ChildProcessJob _job;
    private Process? _process;

    public MediaMtxService(LanAddressService lan, ILogger<MediaMtxService> logger, ChildProcessJob job)
    {
        _lan = lan;
        _logger = logger;
        _job = job;
        PublisherToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
    }

    public string PublisherToken { get; }
    public string PublisherUrl => $"rtsp://obs:{PublisherToken}@127.0.0.1:8554/display";
    public bool IsRunning => _process is { HasExited: false };
    public string? LastError { get; private set; }

    public object GetStatus()
    {
        var endpoint = _lan.GetPrimaryLanEndpoint();
        return new
        {
            running = IsRunning,
            watchUrl = $"http://{endpoint.Address}:8889/display",
            whipUrl = "http://127.0.0.1:8889/display/whip",
            bearerToken = $"obs:{PublisherToken}",
            error = LastError
        };
    }

    public async Task<(bool Success, string Message)> StartAsync()
    {
        if (IsRunning) return (true, "WebRTC geçidi zaten çalışıyor.");

        var executable = FindExecutable();
        if (executable is null) return (false, "MediaMTX bulunamadı. Winget ile bluenviron.mediamtx paketini kurun.");

        try
        {
            var endpoint = _lan.GetPrimaryLanEndpoint();
            var runtimeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DisplayOnWeb");
            Directory.CreateDirectory(runtimeDirectory);
            var configPath = Path.Combine(runtimeDirectory, "mediamtx.runtime.yml");
            File.WriteAllText(configPath, BuildConfig(endpoint.Address, endpoint.Network));

            var process = Process.Start(new ProcessStartInfo(executable, $"\"{configPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = runtimeDirectory
            });
            if (process is null) return (false, "MediaMTX başlatılamadı.");

            _process = process;
            _job.Add(process);
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) _logger.LogInformation("MediaMTX: {Line}", e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _logger.LogWarning("MediaMTX: {Line}", e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Delay(700);
            if (process.HasExited)
            {
                process.Dispose();
                _process = null;
                LastError = "WebRTC geçidi hemen kapandı. 8889 veya 8554 portunu eski bir LANtern süreci kullanıyor olabilir.";
                return (false, LastError);
            }
            LastError = null;
            return (true, "WebRTC geçidi başlatıldı.");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return (false, $"MediaMTX başlatılamadı: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process is null) return;
        if (!process.HasExited)
        {
            process.Kill(true);
            await process.WaitForExitAsync();
        }
        process.Dispose();
    }

    private string BuildConfig(string address, string network) => $$"""
        logLevel: info
        authMethod: internal
        authInternalUsers:
          - user: obs
            pass: {{PublisherToken}}
            ips: ["127.0.0.1/32"]
            permissions:
              - action: publish
                path: display
          - user: any
            pass:
            ips: ["{{network}}"]
            permissions:
              - action: read
                path: display
        rtsp: true
        rtspAddress: 127.0.0.1:8554
        rtspTransports: [tcp]
        rtmp: false
        hls: false
        srt: false
        moq: false
        playback: false
        api: false
        metrics: false
        pprof: false
        webrtc: true
        webrtcAddress: {{address}}:8889
        webrtcLocalUDPAddress: {{address}}:8189
        webrtcAdditionalHosts: ["{{address}}"]
        paths:
          display:
            source: publisher
        """;

    private static string? FindExecutable()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "mediamtx.exe");
        if (File.Exists(bundled)) return bundled;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packageRoot = Path.Combine(local, "Microsoft", "WinGet", "Packages");
        if (Directory.Exists(packageRoot))
        {
            var found = Directory.EnumerateFiles(packageRoot, "mediamtx.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (found is not null) return found;
        }
        return null;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
