using ProductivityTimer.Infrastructure;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class StopwatchViewModel : ViewModelBase
{
    private readonly ITimerService _timerService;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _resetCommand;
    private TimeSpan _elapsed = TimeSpan.Zero;
    private bool _isRunning;
    private bool _isApplyingState;

    public StopwatchViewModel(ITimerService timerService)
    {
        _timerService = timerService;
        _timerService.Interval = TimeSpan.FromSeconds(1);
        _timerService.Tick += OnTick;

        _startCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
        _pauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
        _resetCommand = new RelayCommand(_ => Reset());

        StartCommand = _startCommand;
        PauseCommand = _pauseCommand;
        ResetCommand = _resetCommand;
    }

    public event EventHandler? StateChanged;

    public string ElapsedTimeText => _elapsed.ToString(@"hh\:mm\:ss");

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(RunStateText));
                _startCommand.RaiseCanExecuteChanged();
                _pauseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string RunStateText => IsRunning
        ? "Активен"
        : _elapsed > TimeSpan.Zero ? "На паузе" : "Готов к запуску";

    public RelayCommand StartCommand { get; }

    public RelayCommand PauseCommand { get; }

    public RelayCommand ResetCommand { get; }

    public void ApplyState(AppState appState)
    {
        _isApplyingState = true;

        try
        {
            _timerService.Stop();
            _elapsed = appState.Stopwatch.Elapsed;
            IsRunning = appState.Stopwatch.IsRunning;

            if (IsRunning)
            {
                _timerService.Start();
            }

            OnPropertyChanged(nameof(ElapsedTimeText));
            OnPropertyChanged(nameof(RunStateText));
        }
        finally
        {
            _isApplyingState = false;
        }
    }

    public void UpdateState(AppState appState)
    {
        appState.Stopwatch = new StopwatchState
        {
            Elapsed = _elapsed,
            IsRunning = IsRunning
        };
    }

    public void PauseForAppClosing()
    {
        if (!IsRunning)
        {
            return;
        }

        _timerService.Stop();
        IsRunning = false;
    }

    private void Start()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        _timerService.Start();
        RequestSave();
    }

    private void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        _timerService.Stop();
        IsRunning = false;
        RequestSave();
    }

    private void Reset()
    {
        _timerService.Stop();
        _elapsed = TimeSpan.Zero;
        IsRunning = false;
        OnPropertyChanged(nameof(ElapsedTimeText));
        OnPropertyChanged(nameof(RunStateText));
        RequestSave();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsRunning)
        {
            return;
        }

        _elapsed += TimeSpan.FromSeconds(1);
        OnPropertyChanged(nameof(ElapsedTimeText));
        RequestSave();
    }

    private void RequestSave()
    {
        if (_isApplyingState)
        {
            return;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
