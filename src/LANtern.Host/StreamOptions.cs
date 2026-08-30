namespace LANtern.Host;

public sealed class StreamOptions
{
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 60;
    public int BitrateKbps { get; set; } = 20000;
    public int RtpPort { get; set; } = 55000;
    public string Encoder { get; set; } = "auto";
}

public sealed record StartStreamRequest(int DisplayIndex = 0, int Width = 1920, int Height = 1080, int Fps = 60, int BitrateKbps = 20000, string Encoder = "auto", string ScalingMode = "fill", bool CaptureCursor = true);
public sealed record OperationResult(bool Success, string Message);
