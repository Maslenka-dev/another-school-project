namespace ProductivityTimer.Services;

public interface ITimerService
{
    event EventHandler? Tick;

    TimeSpan Interval { get; set; }

    bool IsRunning { get; }

    void Start();

    void Stop();
}
