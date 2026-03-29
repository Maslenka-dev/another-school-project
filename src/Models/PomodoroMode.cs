namespace ProductivityTimer.Models;

public sealed class PomodoroMode
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public TimeSpan WorkDuration { get; set; }

    public TimeSpan BreakDuration { get; set; }
}
