namespace LANtern.Host.Settings;

public sealed class LanternSettings
{
    public bool StartWithWindows { get; set; }
    public bool AutoConnectVirtualDisplay { get; set; }
    public bool AutoStartStream { get; set; }
    public bool AutoStartWhenMonitorConnect { get; set; } = true;
    public bool StartInTray { get; set; }
    public string PreferredDisplayName { get; set; } = "";
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 60;
    public int BitrateKbps { get; set; } = 15000;
    public string Encoder { get; set; } = "auto";
    public string ScalingMode { get; set; } = "fill";
    public bool CaptureCursor { get; set; } = true;
}
