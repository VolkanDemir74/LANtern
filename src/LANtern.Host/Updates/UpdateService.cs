using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using LANtern.Host.Settings;

namespace LANtern.Host.Updates;

public sealed class UpdateService(IHttpClientFactory httpClientFactory, LanternSettingsService settings, IHostApplicationLifetime lifetime, ILogger<UpdateService> logger)
{
    private const string ReleasesUrl = "https://api.github.com/repos/VolkanDemir74/LANtern/releases?per_page=20";
    private const string InstallerName = "LANtern-Setup-x64.exe";
    private UpdateInfo? _available;
    private readonly object _progressLock = new();
    private UpdateProgress _progress = new(false, 0, "idle");
    private HttpClient Http => httpClientFactory.CreateClient("LANternUpdates");

    public string CurrentVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?.Split('+')[0] ?? "0.0.0";

    public async Task<UpdateCheckResult> CheckAsync(bool manual, CancellationToken ct)
    {
        var preferences = await settings.GetAsync();
        if (!manual && !preferences.CheckForUpdates)
            return new(false, CurrentVersion, null, false, "disabled", null);

        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUrl);
        request.Headers.UserAgent.ParseAdd($"LANtern/{CurrentVersion}");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()) continue;
            var tag = release.GetProperty("tag_name").GetString() ?? "";
            if (!TryParseVersion(tag, out var remote) || !TryParseVersion(CurrentVersion, out var current) || remote <= current) continue;
            if (!manual && string.Equals(preferences.SkippedUpdateVersion, tag, StringComparison.OrdinalIgnoreCase))
                return new(false, CurrentVersion, tag, true, "skipped", release.GetProperty("html_url").GetString());

            string? installerUrl = null;
            string? checksumUrl = null;
            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                var url = asset.GetProperty("browser_download_url").GetString();
                if (string.Equals(name, InstallerName, StringComparison.OrdinalIgnoreCase)) installerUrl = url;
                else if (name?.Contains("sha256", StringComparison.OrdinalIgnoreCase) == true) checksumUrl = url;
            }
            if (installerUrl is null || checksumUrl is null) continue;
            _available = new(tag, release.GetProperty("name").GetString() ?? tag, release.GetProperty("html_url").GetString()!, installerUrl, checksumUrl);
            return new(true, CurrentVersion, tag, false, "available", _available.ReleaseUrl);
        }
        _available = null;
        return new(false, CurrentVersion, null, false, "current", null);
    }

    public async Task<string> DownloadAndInstallAsync(CancellationToken ct)
    {
        var update = _available ?? throw new InvalidOperationException("Önce güncelleme denetimi yapılmalıdır.");
        SetProgress(true, 0, "downloading");
        var folder = Path.Combine(Path.GetTempPath(), "LANtern", "updates", update.Tag.TrimStart('v'));
        Directory.CreateDirectory(folder);
        var installerPath = Path.Combine(folder, InstallerName);
        var checksumPath = installerPath + ".sha256";
        await DownloadAsync(update.InstallerUrl, installerPath, ct, value => SetProgress(true, value * 0.92, "downloading"));
        await DownloadAsync(update.ChecksumUrl, checksumPath, ct, value => SetProgress(true, 92 + value * 0.02, "downloading"));

        SetProgress(true, 95, "verifying");
        var expected = (await File.ReadAllTextAsync(checksumPath, ct)).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        await using var file = File.OpenRead(installerPath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(file, ct));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Güncelleme dosyasının SHA-256 doğrulaması başarısız oldu.");

        SetProgress(true, 100, "launching");
        _ = Task.Run(async () =>
        {
            await Task.Delay(800);
            try
            {
                Process.Start(new ProcessStartInfo(installerPath, "/SILENT /CLOSEAPPLICATIONS /LANTERNAUTOUPDATE") { UseShellExecute = true, Verb = "runas" });
                lifetime.StopApplication();
            }
            catch (Exception ex) { logger.LogError(ex, "Güncelleme kurulumu başlatılamadı."); }
        });
        return update.Tag;
    }

    public UpdateProgress GetProgress()
    {
        lock (_progressLock) return _progress;
    }

    private void SetProgress(bool active, double percent, string stage)
    {
        lock (_progressLock) _progress = new(active, Math.Clamp((int)Math.Round(percent), 0, 100), stage);
    }

    private async Task DownloadAsync(string url, string destination, CancellationToken ct, Action<double> report)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd($"LANtern/{CurrentVersion}");
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var target = File.Create(destination);
        var total = response.Content.Headers.ContentLength;
        var buffer = new byte[128 * 1024];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), ct);
            received += read;
            if (total is > 0) report(received * 100d / total.Value);
        }
        report(100);
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        var normalized = value.Trim().TrimStart('v', 'V').Split('-', 2)[0];
        return Version.TryParse(normalized, out version!);
    }

    private sealed record UpdateInfo(string Tag, string Name, string ReleaseUrl, string InstallerUrl, string ChecksumUrl);
}

public sealed record UpdateCheckResult(bool Available, string CurrentVersion, string? LatestVersion, bool Skipped, string Status, string? ReleaseUrl);
public sealed record UpdateProgress(bool Active, int Percent, string Stage);
