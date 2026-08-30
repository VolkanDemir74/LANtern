using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.IO.Pipes;
using System.Text;

namespace DisplayOnWeb.Host.VirtualDisplay;

public sealed class VirtualDisplayManager
{
    private const string StopEventName = @"Local\LANternVirtualDisplayStop";
    private const uint EventModifyState = 0x0002;
    private readonly ILogger<VirtualDisplayManager> _logger;
    private readonly IConfiguration _configuration;

    public VirtualDisplayManager(ILogger<VirtualDisplayManager> logger, IConfiguration configuration)
        => (_logger, _configuration) = (logger, configuration);

    public bool IsConnected
    {
        get
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE 'SWD%DISPLAYONWEBVIRTUALDISPLAY%'");
            return searcher.Get().Count > 0;
        }
    }

    public async Task<OperationResult> StartAsync()
    {
        if (IsConnected) return new(true, "Sanal monitör zaten bağlı.");
        var serviceResult = await SendServiceCommandAsync("CONNECT");
        if (serviceResult is not null)
        {
            await Task.Delay(1200);
            return IsConnected ? new(true, "Sanal monitör bağlandı.") : new(false, serviceResult);
        }
        var helper = FindHelper();
        if (helper is null) return new(false, "Sanal monitör başlatıcısı bulunamadı.");

        try
        {
            Process.Start(new ProcessStartInfo(helper)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
            await Task.Delay(1800);
            return IsConnected ? new(true, "Sanal monitör bağlandı.") : new(false, "Sanal monitör bağlanamadı.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new(false, "Sanal monitör için yönetici izni iptal edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sanal monitör başlatılamadı");
            return new(false, $"Sanal monitör başlatılamadı: {ex.Message}");
        }
    }

    public async Task<OperationResult> StopAsync()
    {
        var serviceResult = await SendServiceCommandAsync("DISCONNECT");
        if (serviceResult is not null)
        {
            await Task.Delay(700);
            return IsConnected ? new(false, "Sanal monitör kapatılamadı.") : new(true, "Sanal monitör kapatıldı.");
        }
        using var stopEvent = OpenEvent(EventModifyState, false, StopEventName);
        if (!stopEvent.IsInvalid) SetEvent(stopEvent);

        foreach (var process in Process.GetProcessesByName("IddSampleApp"))
        {
            using (process)
            {
                try { process.Kill(true); await process.WaitForExitAsync(); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    _logger.LogWarning("Sanal monitör yardımcısı doğrudan kapatılamadı: {Message}", ex.Message);
                }
            }
        }
        await Task.Delay(500);
        return IsConnected ? new(false, "Sanal monitör kapatılamadı.") : new(true, "Sanal monitör kapatıldı.");
    }

    private async Task<string?> SendServiceCommandAsync(string command)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(".", "LANtern.DeviceService", PipeDirection.InOut, PipeOptions.Asynchronous);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
            await pipe.ConnectAsync(timeout.Token);
            await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync(command);
            var response = await reader.ReadLineAsync(timeout.Token);
            return response?.StartsWith("OK", StringComparison.OrdinalIgnoreCase) == true ? response : response ?? "LANtern cihaz servisi yanıt vermedi.";
        }
        catch (OperationCanceledException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("LANtern cihaz servisine erişilemedi: {Message}", ex.Message);
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern Microsoft.Win32.SafeHandles.SafeWaitHandle OpenEvent(uint desiredAccess, bool inheritHandle, string name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetEvent(Microsoft.Win32.SafeHandles.SafeWaitHandle eventHandle);

    private string? FindHelper()
    {
        var configured = _configuration["VirtualDisplay:HelperPath"];
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;
        var bundled = Path.Combine(AppContext.BaseDirectory, "DisplayOnWeb.VirtualDisplay.Device.exe");
        if (File.Exists(bundled)) return bundled;
        var developmentRelease = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "DisplayOnWeb.VirtualDisplay.Device", "x64", "Release", "IddSampleApp.exe"));
        if (File.Exists(developmentRelease)) return developmentRelease;
        var development = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "x64", "Debug", "IddSampleApp.exe"));
        return File.Exists(development) ? development : null;
    }
}
