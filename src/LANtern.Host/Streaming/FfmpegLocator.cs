using System.Diagnostics;

namespace LANtern.Host.Streaming;

public sealed class FfmpegLocator
{
    private IReadOnlyList<string>? _usableEncoders;

    public string? Find()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(bundled)) return bundled;
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var fromPath = path.Split(Path.PathSeparator).Select(p => Path.Combine(p, "ffmpeg.exe")).FirstOrDefault(File.Exists);
        if (fromPath is not null) return fromPath;

        var wingetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
        return Directory.Exists(wingetRoot)
            ? Directory.EnumerateFiles(wingetRoot, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    public async Task<IReadOnlyList<string>> GetEncodersAsync(string executable, CancellationToken ct)
    {
        using var process = Process.Start(new ProcessStartInfo(executable, "-hide_banner -encoders") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true })!;
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return new[] { "h264_nvenc", "h264_qsv", "h264_amf", "libx264" }.Where(output.Contains).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetUsableEncodersAsync(string executable, CancellationToken ct)
    {
        if (_usableEncoders is not null) return _usableEncoders;
        var listed = await GetEncodersAsync(executable, ct);
        var usable = new List<string>();
        foreach (var encoder in listed)
        {
            if (await CanEncodeAsync(executable, encoder, ct)) usable.Add(encoder);
        }
        _usableEncoders = usable;
        return usable;
    }

    private static async Task<bool> CanEncodeAsync(string executable, string encoder, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var process = Process.Start(new ProcessStartInfo(executable,
            $"-hide_banner -loglevel error -f lavfi -i color=c=black:s=640x360:r=30 -vf format=yuv420p -frames:v 1 -c:v {encoder} -b:v 2M -f null -")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (process is null) return false;
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(true);
            return false;
        }
    }
}
