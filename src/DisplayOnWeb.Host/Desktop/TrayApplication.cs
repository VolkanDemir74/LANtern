using System.Diagnostics;
using DisplayOnWeb.Host.Streaming;
using DisplayOnWeb.Host.VirtualDisplay;
using DisplayOnWeb.Host.Settings;

namespace DisplayOnWeb.Host.Desktop;

public sealed class TrayApplication : IHostedService, IDisposable
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _configuration;
    private readonly StreamCoordinator _stream;
    private readonly VirtualDisplayManager _virtualDisplay;
    private readonly LanternSettingsService _settings;
    private Thread? _thread;
    private NotifyIcon? _icon;
    private Control? _dispatcher;

    public TrayApplication(IHostApplicationLifetime lifetime, IConfiguration configuration,
        StreamCoordinator stream, VirtualDisplayManager virtualDisplay, LanternSettingsService settings)
        => (_lifetime, _configuration, _stream, _virtualDisplay, _settings) = (lifetime, configuration, stream, virtualDisplay, settings);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
        {
            _thread = new Thread(RunTray) { IsBackground = true, Name = "LANtern Tray" };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _ = OpenAdminWhenRequestedAsync();
        });
        return Task.CompletedTask;
    }

    private async Task OpenAdminWhenRequestedAsync()
    {
        var windowsStartup = Environment.GetCommandLineArgs().Any(arg => arg.Equals("--startup", StringComparison.OrdinalIgnoreCase));
        var open = windowsStartup
            ? (await _settings.GetAsync()).OpenPanelOnStartup
            : _configuration.GetValue("HostUi:OpenAdminOnLaunch", false);
        if (open) OpenAdmin();
    }

    private void RunTray()
    {
        _dispatcher = new Control();
        _dispatcher.CreateControl();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Yönetim panelini aç", null, (_, _) => OpenAdmin());
        menu.Items.Add(new ToolStripSeparator());
        var monitor = menu.Items.Add("Sanal monitör");
        var broadcast = menu.Items.Add("Yayın");
        monitor.Click += async (_, _) =>
        {
            if (_virtualDisplay.IsConnected)
            {
                if (_stream.IsRunning) await _stream.StopAsync();
                await _virtualDisplay.StopAsync();
            }
            else await _virtualDisplay.StartAsync();
            UpdateMenu(monitor, broadcast);
        };
        broadcast.Click += async (_, _) =>
        {
            if (_stream.IsRunning) await _stream.StopAsync();
            else OpenAdmin();
            UpdateMenu(monitor, broadcast);
        };
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Hakkında / About", null, (_, _) => ShowAbout());
        menu.Items.Add("GitHub — VolkanDemir74", null, (_, _) => OpenUrl("https://github.com/VolkanDemir74"));
        menu.Items.Add("LANtern'dan çık", null, (_, _) => _lifetime.StopApplication());
        menu.Opening += (_, _) => UpdateMenu(monitor, broadcast);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LANtern.ico");
        var trayIcon = File.Exists(iconPath) ? new Icon(iconPath) : Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
        _icon = new NotifyIcon { Icon = trayIcon, Text = "LANtern", Visible = true, ContextMenuStrip = menu };
        _icon.DoubleClick += (_, _) => OpenAdmin();
        UpdateMenu(monitor, broadcast);
        Application.Run();
    }

    private void UpdateMenu(ToolStripItem monitor, ToolStripItem broadcast)
    {
        monitor.Text = _virtualDisplay.IsConnected ? "✓ Sanal monitör: Bağlı" : "○ Sanal monitör: Bağlı değil";
        broadcast.Text = _stream.IsRunning ? "✓ Yayın: Aktif (durdur)" : "○ Yayın: Pasif (paneli aç)";
    }

    private void OpenAdmin()
    {
        var port = _configuration.GetValue("Server:Port", 5000);
        OpenUrl($"http://127.0.0.1:{port}/admin.html");
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static void ShowAbout()
    {
        MessageBox.Show(
            "LANtern\n\n" +
            "Yerel ağınızda ekran paylaşımı için açık kaynak bir projedir.\n" +
            "Geliştirici: Volkan Demir\n" +
            "Bu projenin geliştirilmesinde bazı noktalarda yapay zeka desteği alınmıştır.\n" +"\n" +
          
            "An open-source project for screen sharing on your local network.\n" +
            "Developer: Volkan Demir\n" +
            "AI assistance was used at certain points during the development of this project.\n" ,
            "LANtern — Hakkında / About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_dispatcher is { IsDisposed: false })
            _dispatcher.BeginInvoke(() => { if (_icon is not null) _icon.Visible = false; Application.ExitThread(); });
        return Task.CompletedTask;
    }

    public void Dispose() { _icon?.Dispose(); _dispatcher?.Dispose(); }
}
