using System.Diagnostics;
using System.Globalization;
using LANtern.Host.Streaming;
using LANtern.Host.VirtualDisplay;
using LANtern.Host.Settings;

namespace LANtern.Host.Desktop;

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
        _lifetime.ApplicationStarted.Register(() => _ = StartDesktopAsync());
        return Task.CompletedTask;
    }

    private async Task StartDesktopAsync()
    {
        var settings = await _settings.GetAsync();
        _thread = new Thread(() => RunTray(settings.StartInTray)) { IsBackground = true, Name = "LANtern Tray" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!settings.StartInTray) OpenAdmin();
    }

    private void RunTray(bool showStartupNotice)
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
        _icon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "LANtern arka planda çalışıyor",
            Visible = true,
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => OpenAdmin();
        UpdateMenu(monitor, broadcast);
        var turkish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase);
        _icon.BalloonTipTitle = turkish ? "LANtern burada!" : "LANtern is here!";
        _icon.BalloonTipText = turkish
            ? "Arka planda çalışıyorum. Yönetim paneli için simgeye çift tıkla."
            : "Running in the background. Double-click the icon to open the control panel.";
        // Windows 11 can discard a balloon requested before the tray message
        // loop is ready, so show it shortly after the loop starts.
        System.Windows.Forms.Timer? startupNotification = null;
        if (showStartupNotice)
        {
            startupNotification = new System.Windows.Forms.Timer { Interval = 1200 };
            startupNotification.Tick += (_, _) =>
            {
                startupNotification.Stop();
                _icon?.ShowBalloonTip(5000);
                ShowStartupNotice(turkish, trayIcon);
                startupNotification.Dispose();
            };
            startupNotification.Start();
        }
        Application.Run();
    }

    private void ShowStartupNotice(bool turkish, Icon icon)
    {
        var notice = new Form
        {
            AutoScaleMode = AutoScaleMode.Dpi,
            BackColor = Color.FromArgb(15, 25, 41),
            ClientSize = new Size(360, 92),
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            TopMost = true
        };

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Cursor.Position);
        notice.Location = new Point(workingArea.Right - notice.Width - 16, workingArea.Bottom - notice.Height - 16);

        var iconView = new PictureBox
        {
            Image = icon.ToBitmap(),
            Location = new Point(18, 22),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(82, 17),
            Text = turkish ? "LANtern burada!" : "LANtern is here!"
        };
        var message = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(174, 199, 232),
            Location = new Point(82, 43),
            Size = new Size(260, 38),
            Text = turkish
                ? "Arka planda çalışıyor. Paneli açmak için tıkla."
                : "Running in the background. Click to open the panel."
        };

        notice.Controls.AddRange([iconView, title, message]);
        void OpenFromNotice(object? _, EventArgs __) { OpenAdmin(); notice.Close(); }
        notice.Click += OpenFromNotice;
        iconView.Click += OpenFromNotice;
        title.Click += OpenFromNotice;
        message.Click += OpenFromNotice;

        var closeTimer = new System.Windows.Forms.Timer { Interval = 6000 };
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            closeTimer.Dispose();
            if (!notice.IsDisposed) notice.Close();
        };
        notice.FormClosed += (_, _) =>
        {
            closeTimer.Stop();
            closeTimer.Dispose();
            iconView.Image?.Dispose();
            notice.Dispose();
        };
        closeTimer.Start();
        notice.Show();
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
            "Geliştirici: Volkan Demir\n\n" +
            "An open-source project for screen sharing on your local network.\n" +
            "Developer: Volkan Demir\n",
            "LANtern - Hakkında / About",
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
