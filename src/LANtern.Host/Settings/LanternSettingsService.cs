using System.Text.Json;
using Microsoft.Win32;

namespace LANtern.Host.Settings;

public sealed class LanternSettingsService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "LANtern";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LANtern", "settings.json");
    private LanternSettings? _settings;

    public async Task<LanternSettings> GetAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_settings is not null) return Clone(_settings);
            _settings = File.Exists(_path)
                ? JsonSerializer.Deserialize<LanternSettings>(await File.ReadAllTextAsync(_path)) ?? new()
                : new();
            return Clone(_settings);
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(LanternSettings settings)
    {
        Validate(settings);
        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            SetWindowsStartup(settings.StartWithWindows);
            _settings = Clone(settings);
        }
        finally { _gate.Release(); }
    }

    private static void SetWindowsStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (!enabled) { key?.DeleteValue(RunValueName, false); return; }
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("Uygulama yolu bulunamadı.");
        var command = Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? $"\"{processPath}\" \"{typeof(LanternSettingsService).Assembly.Location}\" --startup"
            : $"\"{processPath}\" --startup";
        key?.SetValue(RunValueName, command, RegistryValueKind.String);
    }

    private static void Validate(LanternSettings value)
    {
        value.Width = Math.Clamp(value.Width, 640, 7680);
        value.Height = Math.Clamp(value.Height, 480, 4320);
        value.Fps = Math.Clamp(value.Fps, 15, 120);
        value.BitrateKbps = Math.Clamp(value.BitrateKbps, 1000, 100000);
        value.Encoder = string.IsNullOrWhiteSpace(value.Encoder) ? "auto" : value.Encoder;
        value.ScalingMode = value.ScalingMode == "fit" ? "fit" : "fill";
        value.ControlPanelClient = value.ControlPanelClient is "chrome" or "edge" ? value.ControlPanelClient : "native";
    }

    private static LanternSettings Clone(LanternSettings value) => JsonSerializer.Deserialize<LanternSettings>(JsonSerializer.Serialize(value))!;
}
