using System;
using System.Windows.Threading;

namespace ProductivityTimer.Services;

public sealed class TimerService : ITimerService
{
    private readonly DispatcherTimer _dispatcherTimer;

    public TimerService()
    {
        _dispatcherTimer = new DispatcherTimer();
        _dispatcherTimer.Tick += OnTick;
        _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
    }

    public event EventHandler? Tick;

    public TimeSpan Interval
    {
        get => _dispatcherTimer.Interval;
        set => _dispatcherTimer.Interval = value;
    }

    public bool IsRunning => _dispatcherTimer.IsEnabled;

    public void Start()
    {
        _dispatcherTimer.Start();
    }

    public void Stop()
    {
        _dispatcherTimer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}
