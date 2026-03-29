using ProductivityTimer.Models;

namespace ProductivityTimer.Services;

public interface IPomodoroService
{
    event EventHandler? StateChanged;

    event EventHandler? StatisticsChanged;

    PomodoroState CurrentState { get; }

    StatisticsModel CurrentStatistics { get; }

    IReadOnlyList<PomodoroMode> GetAvailableModes();

    PomodoroMode GetMode(string? modeId);

    void RestoreState(PomodoroState state, StatisticsModel statistics);

    void SelectMode(string modeId);

    void Start();

    void Pause();

    void PauseForAppClosing();

    void Reset();

    bool RefreshStatisticsForToday();
}
