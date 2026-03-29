namespace ProductivityTimer.Models;

public sealed class TimerState
{
    public TimeSpan InitialDuration { get; set; } = TimeSpan.Zero;

    public TimeSpan RemainingTime { get; set; } = TimeSpan.Zero;

    public bool IsRunning { get; set; }
}
