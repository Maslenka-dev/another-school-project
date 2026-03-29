namespace ProductivityTimer.Models;

public sealed class StopwatchState
{
    public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

    public bool IsRunning { get; set; }
}
