using System.Diagnostics;

namespace DisplayOnWeb.Host.Streaming;

public sealed class FfmpegLocator
{
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
}
