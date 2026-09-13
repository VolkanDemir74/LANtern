using System.Diagnostics;
using System.Globalization;
using LANtern.Host.Capture;
using Microsoft.Extensions.Options;

namespace LANtern.Host.Streaming;

public sealed class StreamCoordinator : IDisposable
{
    private readonly DisplayCatalog _displays;
    private readonly FfmpegLocator _ffmpeg;
    private readonly StreamOptions _defaults;
    private readonly ILogger<StreamCoordinator> _logger;
    private readonly MediaMtxService _mediaMtx;
    private readonly ChildProcessJob _job;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private Process? _process;
    private StartStreamRequest? _lastRequest;
    private bool _shouldBeRunning;

    public bool IsRunning => _process is { HasExited: false };
    public bool ShouldBeRunning => Volatile.Read(ref _shouldBeRunning);
    public DisplayInfo? SelectedDisplay { get; private set; }
    public string ActiveEncoder { get; private set; } = "Başlatılmadı";
    public string? LastError { get; private set; }
    public long ReceivedPackets => Interlocked.Read(ref _receivedPackets);
    public double EncodeFps => Volatile.Read(ref _encodeFps);
    public double EncodeSpeed => Volatile.Read(ref _encodeSpeed);
    public long DroppedFrames => Interlocked.Read(ref _droppedFrames);
    public long DuplicatedFrames => Interlocked.Read(ref _duplicatedFrames);

    public StreamCoordinator(DisplayCatalog displays, FfmpegLocator ffmpeg, IOptions<StreamOptions> defaults, ILogger<StreamCoordinator> logger, MediaMtxService mediaMtx, ChildProcessJob job)
        => (_displays, _ffmpeg, _defaults, _logger, _mediaMtx, _job) = (displays, ffmpeg, defaults.Value, logger, mediaMtx, job);

    public async Task<OperationResult> StartAsync(StartStreamRequest request, CancellationToken ct)
    {
        await _operationGate.WaitAsync(ct);
        try
        {
            Volatile.Write(ref _shouldBeRunning, false);
            await StopProcessAsync();
            var result = await StartCoreAsync(request, ct);
            if (result.Success)
            {
                _lastRequest = request;
                Volatile.Write(ref _shouldBeRunning, true);
            }
            return result;
        }
        finally { _operationGate.Release(); }
    }

    private async Task<OperationResult> StartCoreAsync(StartStreamRequest request, CancellationToken ct)
    {
        _job.StopOrphanedLANternProcesses();
        LastError = null;
        Interlocked.Exchange(ref _receivedPackets, 0);
        Volatile.Write(ref _encodeFps, 0);
        Volatile.Write(ref _encodeSpeed, 0);
        Interlocked.Exchange(ref _droppedFrames, 0);
        Interlocked.Exchange(ref _duplicatedFrames, 0);
        var executable = _ffmpeg.Find();
        if (executable is null) return Fail("ffmpeg.exe bulunamadı. README'deki FFmpeg Shared kurulumunu yapın veya ffmpeg.exe dosyasını uygulamanın yanına koyun.");

        var displays = _displays.GetDisplays();
        if (request.DisplayIndex < 0 || request.DisplayIndex >= displays.Count) return Fail("Geçersiz ekran seçimi.");
        SelectedDisplay = displays[request.DisplayIndex];

        var available = await _ffmpeg.GetUsableEncodersAsync(executable, ct);
        var selectedEncoder = ChooseEncoder(request.Encoder, available);
        if (selectedEncoder is null)
            return Fail($"Seçilen video kodlayıcı ({request.Encoder}) bu bilgisayarda kullanılamıyor. Otomatik kodlayıcıyı deneyin.");
        ActiveEncoder = selectedEncoder;
        var gateway = await _mediaMtx.StartAsync();
        if (!gateway.Success) return Fail(gateway.Message);

        var args = BuildArguments(SelectedDisplay, request, ActiveEncoder, _mediaMtx.PublisherUrl);
        _logger.LogInformation("FFmpeg başlatılıyor: {Executable} {Arguments}", executable, args);
        var process = Process.Start(new ProcessStartInfo(executable, args)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
        });
        if (process is null) return Fail("FFmpeg başlatılamadı.");
        _process = process;
        _job.Add(process);
        _ = MonitorAsync(process);
        await Task.Delay(800, ct);
        return process.HasExited ? Fail(LastError ?? "FFmpeg erken kapandı.") : new(true, $"{SelectedDisplay.Name}, {ActiveEncoder} ile yayın başladı.");
    }

    public async Task StopAsync()
    {
        await _operationGate.WaitAsync();
        try
        {
            Volatile.Write(ref _shouldBeRunning, false);
            _lastRequest = null;
            await StopProcessAsync();
        }
        finally { _operationGate.Release(); }
    }

    public async Task<OperationResult?> TryRecoverAsync(CancellationToken ct)
    {
        if (!ShouldBeRunning || IsRunning) return null;
        await _operationGate.WaitAsync(ct);
        try
        {
            if (!ShouldBeRunning || IsRunning || _lastRequest is not { } request) return null;
            _logger.LogWarning("Yayın işlemi beklenmedik biçimde durdu; aynı profille yeniden başlatılıyor.");
            await StopProcessAsync();
            return await StartCoreAsync(request, ct);
        }
        finally { _operationGate.Release(); }
    }

    private async Task StopProcessAsync()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process is { HasExited: false })
        {
            process.Kill(true);
            await process.WaitForExitAsync();
        }
        process?.Dispose();
    }

    private async Task MonitorAsync(Process process)
    {
        var recentErrors = new Queue<string>();
        try
        {
            while (await process.StandardError.ReadLineAsync() is { } line)
            {
                if (TryReadProgress(line)) continue;
                if (recentErrors.Count >= 40) recentErrors.Dequeue();
                recentErrors.Enqueue(line);
            }
            await process.WaitForExitAsync();
            // Stop/restart intentionally closes FFmpeg's RTSP pipe. Only report
            // an encoder failure when the exited process is still the active one.
            if (process.ExitCode != 0 && ReferenceEquals(Volatile.Read(ref _process), process))
            {
                var details = string.Join(" | ", recentErrors.Where(line => !string.IsNullOrWhiteSpace(line)).TakeLast(3));
                LastError = string.IsNullOrWhiteSpace(details)
                    ? $"Video kodlayıcı ({ActiveEncoder}) beklenmedik biçimde kapandı."
                    : $"Video kodlayıcı ({ActiveEncoder}) başlatılamadı: {details}";
                _logger.LogError("FFmpeg hata çıktısı: {Error}", string.Join(Environment.NewLine, recentErrors));
                Interlocked.CompareExchange(ref _process, null, process);
            }
        }
        catch (ObjectDisposedException) { }
    }

    private bool TryReadProgress(string line)
    {
        var separator = line.IndexOf('=');
        if (separator < 1) return false;
        var key = line[..separator];
        var value = line[(separator + 1)..];
        switch (key)
        {
            case "fps" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps):
                Volatile.Write(ref _encodeFps, fps); return true;
            case "speed":
                value = value.TrimEnd('x');
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var speed))
                    Volatile.Write(ref _encodeSpeed, speed);
                return true;
            case "drop_frames" when long.TryParse(value, out var dropped):
                Interlocked.Exchange(ref _droppedFrames, dropped); return true;
            case "dup_frames" when long.TryParse(value, out var duplicated):
                Interlocked.Exchange(ref _duplicatedFrames, duplicated); return true;
            case "frame" or "bitrate" or "total_size" or "out_time_us" or "out_time_ms" or "out_time" or "progress":
                return true;
            default:
                return false;
        }
    }

    private static string? ChooseEncoder(string requested, IReadOnlyList<string> available)
    {
        if (string.Equals(requested, "auto", StringComparison.OrdinalIgnoreCase)) return available.FirstOrDefault();
        return available.Contains(requested) ? requested : null;
    }

    private static string BuildArguments(DisplayInfo display, StartStreamRequest request, string encoder, string publisherUrl)
    {
        // One IDR per second avoids periodic bitrate and frame-time spikes.
        var keyFrameInterval = Math.Max(1, request.Fps);
        var common = $"-hide_banner -loglevel warning -progress pipe:2 -stats_period 1 -an -c:v {encoder} -b:v {request.BitrateKbps}k -maxrate {request.BitrateKbps}k -bufsize {Math.Max(1000, request.BitrateKbps / 2)}k -g {keyFrameInterval} -keyint_min {keyFrameInterval} -sc_threshold 0 -bf 0 -flags +low_delay";
        var tuning = encoder switch
        {
            "h264_nvenc" => "-preset p3 -tune ull -rc cbr -multipass disabled -delay 0 -surfaces 4 -profile:v baseline -forced-idr 1 -zerolatency 1 -aud 1",
            "h264_qsv" => "-preset veryfast -low_power 1 -profile:v baseline",
            "h264_amf" => "-usage ultralowlatency -quality speed -profile:v baseline",
            _ => "-preset ultrafast -tune zerolatency -profile:v baseline"
        };
        // Desktop Duplication keeps the frame and cursor composition on the GPU.
        // With a native-size NVENC stream no GPU-to-CPU copy or software scaling is needed.
        var directGpuCapture = encoder == "h264_nvenc" && display.Width == request.Width && display.Height == request.Height;
        if (directGpuCapture)
        {
            var ddaInput = $"-f lavfi -i \"ddagrab=output_idx={display.Index}:draw_mouse={(request.CaptureCursor ? 1 : 0)}:framerate={request.Fps}:output_fmt=8bit\"";
            return $"{ddaInput} {common} {tuning} -bsf:v dump_extra=freq=keyframe -rtsp_transport tcp -muxdelay 0 -f rtsp \"{publisherUrl}\"";
        }

        // Compatibility fallback for software encoders and resized captures.
        var input = $"-f gdigrab -draw_mouse {(request.CaptureCursor ? 1 : 0)} -framerate {request.Fps} -offset_x {display.X} -offset_y {display.Y} -video_size {display.Width}x{display.Height} -i desktop";
        var scale = string.Equals(request.ScalingMode, "fit", StringComparison.OrdinalIgnoreCase)
            ? $"-vf scale={request.Width}:{request.Height}:force_original_aspect_ratio=decrease,pad={request.Width}:{request.Height}:(ow-iw)/2:(oh-ih)/2"
            : $"-vf scale={request.Width}:{request.Height}:force_original_aspect_ratio=increase,crop={request.Width}:{request.Height}";
        return $"{input} {scale} {common} {tuning} -pix_fmt yuv420p -bsf:v dump_extra=freq=keyframe -rtsp_transport tcp -muxdelay 0 -f rtsp \"{publisherUrl}\"";
    }

    private OperationResult Fail(string message) { LastError = message; return new(false, message); }
    private long _receivedPackets;
    private double _encodeFps;
    private double _encodeSpeed;
    private long _droppedFrames;
    private long _duplicatedFrames;
    public void Dispose() => StopAsync().GetAwaiter().GetResult();
}
