using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LANtern.Host.Desktop;

public sealed class AdminWindow : Form
{
    private readonly WebView2 _browser = new() { Dock = DockStyle.Fill };
    private readonly Uri _address;
    private bool _initialized;

    public AdminWindow(Uri address)
    {
        _address = address;
        Text = "LANtern";
        Icon = LoadIcon();
        MinimumSize = new Size(1000, 650);
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Cursor.Position);
        var defaultWidth = Math.Clamp((int)Math.Round(workingArea.Width * 0.48), 1200, 1640);
        var defaultHeight = Math.Clamp((int)Math.Round(workingArea.Height * 0.72), 800, 1040);
        Size = new Size(
            Math.Min(defaultWidth, workingArea.Width - 32),
            Math.Min(defaultHeight, workingArea.Height - 32));
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_browser);
        BackColor = Color.FromArgb(9, 13, 20);
        HandleCreated += (_, _) => EnableDarkTitleBar();
        Shown += async (_, _) => await InitializeBrowserAsync();
        FormClosing += (_, args) =>
        {
            if (args.CloseReason is CloseReason.UserClosing)
            {
                args.Cancel = true;
                Hide();
            }
        };
    }

    public void ShowAndActivate()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private async Task InitializeBrowserAsync()
    {
        if (_initialized) return;
        try
        {
            await _browser.EnsureCoreWebView2Async();
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _browser.Source = _address;
            _initialized = true;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "Microsoft Edge WebView2 Runtime bulunamadı. Yönetim paneli varsayılan tarayıcıda açılacak.\n\nMicrosoft Edge WebView2 Runtime was not found. The control panel will open in your default browser.",
                "LANtern", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.Start(new ProcessStartInfo(_address.ToString()) { UseShellExecute = true });
            Hide();
        }
    }

    private static Icon? LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "LANtern.ico");
        return File.Exists(path) ? new Icon(path) : Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
    }

    private void EnableDarkTitleBar()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;
        var enabled = 1;
        // Attribute 20 is current on Windows 10 20H1+ and Windows 11; 19 covers older Windows 10 builds.
        if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
}
