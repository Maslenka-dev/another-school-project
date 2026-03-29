using System.Collections.ObjectModel;
using ProductivityTimer.Infrastructure;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class PomodoroViewModel : ViewModelBase
{
    private readonly IPomodoroService _pomodoroService;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _resetCommand;
    private PomodoroMode? _selectedMode;
    private string _currentPhase = "Учёба";
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private int _cycleNumber = 1;
    private bool _isRunning;
    private bool _isSynchronizingFromService;

    public PomodoroViewModel(IPomodoroService pomodoroService)
    {
        _pomodoroService = pomodoroService;
        _pomodoroService.StateChanged += OnPomodoroStateChanged;

        AvailableModes = new ObservableCollection<PomodoroMode>(_pomodoroService.GetAvailableModes());

        _startCommand = new RelayCommand(_ => _pomodoroService.Start(), _ => !IsRunning);
        _pauseCommand = new RelayCommand(_ => _pomodoroService.Pause(), _ => IsRunning);
        _resetCommand = new RelayCommand(_ => _pomodoroService.Reset());

        StartCommand = _startCommand;
        PauseCommand = _pauseCommand;
        ResetCommand = _resetCommand;

        SyncFromService();
    }

    public event EventHandler? StateChanged;

    public ObservableCollection<PomodoroMode> AvailableModes { get; }

    public PomodoroMode? SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (value is null || !SetProperty(ref _selectedMode, value) || _isSynchronizingFromService)
            {
                return;
            }

            _pomodoroService.SelectMode(value.Id);
        }
    }

    public string CurrentPhase
    {
        get => _currentPhase;
        private set => SetProperty(ref _currentPhase, value);
    }

    public string RemainingTimeText => _remainingTime.ToString(@"hh\:mm\:ss");

    public int CycleNumber
    {
        get => _cycleNumber;
        private set => SetProperty(ref _cycleNumber, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(IsModeSelectionEnabled));
                OnPropertyChanged(nameof(RunStateText));
                _startCommand.RaiseCanExecuteChanged();
                _pauseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsModeSelectionEnabled => !IsRunning;

    public string RunStateText
    {
        get
        {
            if (IsRunning)
            {
                return "Активен";
            }

            if (SelectedMode is null)
            {
                return "Готов к запуску";
            }

            var phaseDuration = CurrentPhase == "Перерыв"
                ? SelectedMode.BreakDuration
                : SelectedMode.WorkDuration;

            return _remainingTime == phaseDuration
                ? "Готов к запуску"
                : "На паузе";
        }
    }

    public RelayCommand StartCommand { get; }

    public RelayCommand PauseCommand { get; }

    public RelayCommand ResetCommand { get; }

    public void ApplyState(AppState appState)
    {
        _pomodoroService.RestoreState(appState.Pomodoro, appState.Statistics);
        SyncFromService();
    }

    public void UpdateState(AppState appState)
    {
        var state = _pomodoroService.CurrentState;

        appState.Pomodoro = new PomodoroState
        {
            ModeId = state.ModeId,
            CurrentPhase = state.CurrentPhase,
            RemainingTime = state.RemainingTime,
            CycleNumber = state.CycleNumber,
            IsRunning = state.IsRunning
        };
    }

    public void PauseForAppClosing()
    {
        _pomodoroService.PauseForAppClosing();
        SyncFromService();
    }

    private void OnPomodoroStateChanged(object? sender, EventArgs e)
    {
        SyncFromService();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncFromService()
    {
        var state = _pomodoroService.CurrentState;
        var mode = _pomodoroService.GetMode(state.ModeId);

        _isSynchronizingFromService = true;
        SelectedMode = AvailableModes.FirstOrDefault(item => item.Id == mode.Id) ?? mode;
        _isSynchronizingFromService = false;

        CurrentPhase = GetPhaseDisplayName(state.CurrentPhase);
        _remainingTime = state.RemainingTime;
        OnPropertyChanged(nameof(RemainingTimeText));
        CycleNumber = state.CycleNumber;
        IsRunning = state.IsRunning;
        OnPropertyChanged(nameof(RunStateText));
    }

    private static string GetPhaseDisplayName(string phase)
    {
        return phase switch
        {
            "Work" => "Учёба",
            "Break" => "Перерыв",
            _ => phase
        };
    }
}
