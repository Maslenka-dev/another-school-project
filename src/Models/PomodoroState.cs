namespace ProductivityTimer.Models;

public sealed class PomodoroState
{
    public string ModeId { get; set; } = "25/5";

    public string CurrentPhase { get; set; } = "Work";

    public TimeSpan RemainingTime { get; set; } = TimeSpan.FromMinutes(25);

    public int CycleNumber { get; set; } = 1;

    public bool IsRunning { get; set; }
}
