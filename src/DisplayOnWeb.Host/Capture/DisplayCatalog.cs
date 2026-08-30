using System.Windows.Forms;

namespace DisplayOnWeb.Host.Capture;

public sealed record DisplayInfo(int Index, string Name, int X, int Y, int Width, int Height, bool Primary);

public sealed class DisplayCatalog
{
    public IReadOnlyList<DisplayInfo> GetDisplays() => Screen.AllScreens.Select((screen, index) => new DisplayInfo(
        index,
        string.IsNullOrWhiteSpace(screen.DeviceName) ? $"Ekran {index + 1}" : screen.DeviceName,
        screen.Bounds.X,
        screen.Bounds.Y,
        screen.Bounds.Width,
        screen.Bounds.Height,
        screen.Primary)).ToArray();
}
