namespace LANtern.Host.Desktop;

public sealed class SingleInstanceActivationService(TrayApplication tray) : BackgroundService
{
    public const string EventName = @"Local\LANtern-VolkanDemir74-Activate";

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() =>
    {
        using var activation = EventWaitHandle.OpenExisting(EventName);
        var handles = new[] { activation, stoppingToken.WaitHandle };
        while (!stoppingToken.IsCancellationRequested)
        {
            if (WaitHandle.WaitAny(handles) != 0) break;
            tray.ShowAdmin();
        }
    }, stoppingToken);
}
