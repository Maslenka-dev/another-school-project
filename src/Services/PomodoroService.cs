using ProductivityTimer.Models;

namespace ProductivityTimer.Services;

public sealed class PomodoroService : IPomodoroService
{
    private const string WorkPhase = "Work";
    private const string BreakPhase = "Break";

    private readonly IReadOnlyList<PomodoroMode> _availableModes = new List<PomodoroMode>
    {
        new()
        {
            Id = "25/5",
            DisplayName = "25 / 5",
            WorkDuration = TimeSpan.FromMinutes(25),
            BreakDuration = TimeSpan.FromMinutes(5)
        },
        new()
        {
            Id = "50/10",
            DisplayName = "50 / 10",
            WorkDuration = TimeSpan.FromMinutes(50),
            BreakDuration = TimeSpan.FromMinutes(10)
        },
        new()
        {
            Id = "60/10",
            DisplayName = "60 / 10",
            WorkDuration = TimeSpan.FromMinutes(60),
            BreakDuration = TimeSpan.FromMinutes(10)
        }
    };

    private readonly ITimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;
    private PomodoroState _currentState;
    private StatisticsModel _currentStatistics;

    public PomodoroService(ITimerService timerService, INotificationService notificationService, ITimeProvider timeProvider)
    {
        _timerService = timerService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;

        _timerService.Interval = TimeSpan.FromSeconds(1);
        _timerService.Tick += OnTick;

        var defaultMode = _availableModes[0];
        _currentState = CreateInitialState(defaultMode);
        _currentStatistics = CreateInitialStatistics();
    }

    public event EventHandler? StateChanged;

    public event EventHandler? StatisticsChanged;

    public PomodoroState CurrentState => CloneState(_currentState);

    public StatisticsModel CurrentStatistics => CloneStatistics(_currentStatistics);

    public IReadOnlyList<PomodoroMode> GetAvailableModes()
    {
        return _availableModes;
    }

    public PomodoroMode GetMode(string? modeId)
    {
        return _availableModes.FirstOrDefault(mode => mode.Id == modeId) ?? _availableModes[0];
    }

    public void RestoreState(PomodoroState state, StatisticsModel statistics)
    {
        var mode = GetMode(state.ModeId);
        EnsureCurrentStatisticsDate(statistics);

        _currentState = NormalizeState(state, mode);
        _currentStatistics = CloneStatistics(statistics);

        if (_currentState.IsRunning)
        {
            _timerService.Start();
        }
        else
        {
            _timerService.Stop();
        }

        RaiseStatisticsChanged();
        RaiseStateChanged();
    }

    public void SelectMode(string modeId)
    {
        var mode = GetMode(modeId);

        _timerService.Stop();
        _currentState = CreateInitialState(mode);
        RaiseStateChanged();
    }

    public void Start()
    {
        RefreshStatisticsForToday();

        if (_currentState.IsRunning)
        {
            return;
        }

        var mode = GetMode(_currentState.ModeId);
        if (_currentState.CurrentPhase == WorkPhase && _currentState.RemainingTime <= TimeSpan.Zero)
        {
            _currentState.RemainingTime = mode.WorkDuration;
        }

        if (_currentState.CurrentPhase == BreakPhase && _currentState.RemainingTime <= TimeSpan.Zero)
        {
            _currentState.RemainingTime = mode.BreakDuration;
        }

        _currentState.IsRunning = true;
        _timerService.Start();
        RaiseStateChanged();
    }

    public void Pause()
    {
        if (!_currentState.IsRunning)
        {
            return;
        }

        _currentState.IsRunning = false;
        _timerService.Stop();
        RaiseStateChanged();
    }

    public void PauseForAppClosing()
    {
        if (!_currentState.IsRunning)
        {
            return;
        }

        _currentState.IsRunning = false;
        _timerService.Stop();
    }

    public void Reset()
    {
        var mode = GetMode(_currentState.ModeId);

        _timerService.Stop();
        _currentState = CreateInitialState(mode);
        RaiseStateChanged();
    }

    public bool RefreshStatisticsForToday()
    {
        var statisticsReset = EnsureCurrentStatisticsDate(_currentStatistics);
        if (statisticsReset)
        {
            RaiseStatisticsChanged();
        }

        return statisticsReset;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        RefreshStatisticsForToday();

        if (!_currentState.IsRunning)
        {
            return;
        }

        _currentState.RemainingTime -= TimeSpan.FromSeconds(1);
        if (_currentState.RemainingTime > TimeSpan.Zero)
        {
            RaiseStateChanged();
            return;
        }

        _currentState.RemainingTime = TimeSpan.Zero;
        CompleteCurrentPhase();
    }

    private void CompleteCurrentPhase()
    {
        var mode = GetMode(_currentState.ModeId);

        if (_currentState.CurrentPhase == WorkPhase)
        {
            _currentStatistics.CompletedPomodoroCount += 1;
            _currentStatistics.TotalWorkTime += mode.WorkDuration;

            _notificationService.Show(
                "Pomodoro",
                $"Рабочая сессия {mode.DisplayName} завершена. Начинается перерыв.");

            _currentState.CurrentPhase = BreakPhase;
            _currentState.RemainingTime = mode.BreakDuration;
            _currentState.IsRunning = true;
            _timerService.Start();

            RaiseStatisticsChanged();
            RaiseStateChanged();
            return;
        }

        _notificationService.Show(
            "Pomodoro",
            "Перерыв завершён. Нажмите Start, чтобы начать следующий рабочий цикл.");

        _currentState.CurrentPhase = WorkPhase;
        _currentState.RemainingTime = mode.WorkDuration;
        _currentState.CycleNumber += 1;
        _currentState.IsRunning = false;
        _timerService.Stop();

        RaiseStateChanged();
    }

    private bool EnsureCurrentStatisticsDate(StatisticsModel statistics)
    {
        var today = _timeProvider.Today;
        if (statistics.StatisticsDate == today)
        {
            return false;
        }

        statistics.StatisticsDate = today;
        statistics.CompletedPomodoroCount = 0;
        statistics.TotalWorkTime = TimeSpan.Zero;
        return true;
    }

    private PomodoroState NormalizeState(PomodoroState state, PomodoroMode mode)
    {
        var phase = state.CurrentPhase == BreakPhase ? BreakPhase : WorkPhase;
        var maxDuration = phase == BreakPhase ? mode.BreakDuration : mode.WorkDuration;
        var remainingTime = state.RemainingTime;

        if (remainingTime <= TimeSpan.Zero || remainingTime > maxDuration)
        {
            remainingTime = maxDuration;
        }

        return new PomodoroState
        {
            ModeId = mode.Id,
            CurrentPhase = phase,
            RemainingTime = remainingTime,
            CycleNumber = state.CycleNumber <= 0 ? 1 : state.CycleNumber,
            IsRunning = state.IsRunning
        };
    }

    private PomodoroState CreateInitialState(PomodoroMode mode)
    {
        return new PomodoroState
        {
            ModeId = mode.Id,
            CurrentPhase = WorkPhase,
            RemainingTime = mode.WorkDuration,
            CycleNumber = 1,
            IsRunning = false
        };
    }

    private StatisticsModel CreateInitialStatistics()
    {
        return new StatisticsModel
        {
            StatisticsDate = _timeProvider.Today,
            CompletedPomodoroCount = 0,
            TotalWorkTime = TimeSpan.Zero
        };
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseStatisticsChanged()
    {
        StatisticsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static PomodoroState CloneState(PomodoroState state)
    {
        return new PomodoroState
        {
            ModeId = state.ModeId,
            CurrentPhase = state.CurrentPhase,
            RemainingTime = state.RemainingTime,
            CycleNumber = state.CycleNumber,
            IsRunning = state.IsRunning
        };
    }

    private static StatisticsModel CloneStatistics(StatisticsModel statistics)
    {
        return new StatisticsModel
        {
            StatisticsDate = statistics.StatisticsDate,
            CompletedPomodoroCount = statistics.CompletedPomodoroCount,
            TotalWorkTime = statistics.TotalWorkTime
        };
    }
}
