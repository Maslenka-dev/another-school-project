using ProductivityTimer.Infrastructure;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class TimerViewModel : ViewModelBase
{
    private readonly ITimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _resetCommand;
    private string _hoursInput = string.Empty;
    private string _minutesInput = string.Empty;
    private string _secondsInput = string.Empty;
    private TimeSpan _initialDuration = TimeSpan.Zero;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private bool _isRunning;
    private bool _isApplyingState;

    public TimerViewModel(ITimerService timerService, INotificationService notificationService)
    {
        _timerService = timerService;
        _notificationService = notificationService;

        _timerService.Interval = TimeSpan.FromSeconds(1);
        _timerService.Tick += OnTick;

        _startCommand = new RelayCommand(_ => Start(), _ => _initialDuration > TimeSpan.Zero && !IsRunning);
        _pauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
        _resetCommand = new RelayCommand(_ => Reset());

        StartCommand = _startCommand;
        PauseCommand = _pauseCommand;
        ResetCommand = _resetCommand;
    }

    public event EventHandler? StateChanged;

    public string HoursInput
    {
        get => _hoursInput;
        set
        {
            if (SetProperty(ref _hoursInput, NormalizeTimeInput(value)))
            {
                UpdateDurationFromInputs();
            }
        }
    }

    public string MinutesInput
    {
        get => _minutesInput;
        set
        {
            if (SetProperty(ref _minutesInput, NormalizeTimeInput(value)))
            {
                UpdateDurationFromInputs();
            }
        }
    }

    public string SecondsInput
    {
        get => _secondsInput;
        set
        {
            if (SetProperty(ref _secondsInput, NormalizeTimeInput(value)))
            {
                UpdateDurationFromInputs();
            }
        }
    }

    public string DisplayTimeText => _remainingTime.ToString(@"hh\:mm\:ss");

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
                OnPropertyChanged(nameof(IsStartEnabled));
                OnPropertyChanged(nameof(RunStateText));
                _startCommand.RaiseCanExecuteChanged();
                _pauseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsInputEnabled => !IsRunning;

    public bool IsStartEnabled => !IsRunning && _initialDuration > TimeSpan.Zero;

    public string RunStateText
    {
        get
        {
            if (IsRunning)
            {
                return "Активен";
            }

            if (_initialDuration <= TimeSpan.Zero)
            {
                return "Ожидает настройки";
            }

            if (_remainingTime <= TimeSpan.Zero)
            {
                return "Завершен";
            }

            return _remainingTime == _initialDuration ? "Готов к запуску" : "На паузе";
        }
    }

    public RelayCommand StartCommand { get; }

    public RelayCommand PauseCommand { get; }

    public RelayCommand ResetCommand { get; }

    public void ApplyState(AppState appState)
    {
        _isApplyingState = true;

        try
        {
            _timerService.Stop();

            _initialDuration = appState.Timer.InitialDuration;
            _remainingTime = appState.Timer.RemainingTime;
            ApplyInputsFromDuration(_initialDuration);

            IsRunning = appState.Timer.IsRunning && _remainingTime > TimeSpan.Zero;
            if (IsRunning)
            {
                _timerService.Start();
            }

            OnPropertyChanged(nameof(DisplayTimeText));
            OnPropertyChanged(nameof(RunStateText));
            OnPropertyChanged(nameof(IsStartEnabled));
            _startCommand.RaiseCanExecuteChanged();
        }
        finally
        {
            _isApplyingState = false;
        }
    }

    public void UpdateState(AppState appState)
    {
        appState.Timer = new TimerState
        {
            InitialDuration = _initialDuration,
            RemainingTime = _remainingTime,
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
        if (_remainingTime <= TimeSpan.Zero)
        {
            _remainingTime = _initialDuration;
            OnPropertyChanged(nameof(DisplayTimeText));
        }

        if (_remainingTime <= TimeSpan.Zero)
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
        _remainingTime = _initialDuration;
        IsRunning = false;
        OnPropertyChanged(nameof(DisplayTimeText));
        OnPropertyChanged(nameof(RunStateText));
        OnPropertyChanged(nameof(IsStartEnabled));
        RequestSave();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsRunning)
        {
            return;
        }

        _remainingTime -= TimeSpan.FromSeconds(1);
        if (_remainingTime > TimeSpan.Zero)
        {
            OnPropertyChanged(nameof(DisplayTimeText));
            RequestSave();
            return;
        }

        _remainingTime = TimeSpan.Zero;
        _timerService.Stop();
        IsRunning = false;
        OnPropertyChanged(nameof(DisplayTimeText));
        OnPropertyChanged(nameof(RunStateText));
        OnPropertyChanged(nameof(IsStartEnabled));
        _notificationService.Show("Таймер", "Заданное время истекло.");
        RequestSave();
    }

    private void UpdateDurationFromInputs()
    {
        if (_isApplyingState || IsRunning)
        {
            return;
        }

        var hours = Math.Clamp(ParsePositiveValue(HoursInput), 0, 60);
        var minutes = Math.Clamp(ParsePositiveValue(MinutesInput), 0, 60);
        var seconds = Math.Clamp(ParsePositiveValue(SecondsInput), 0, 60);

        _initialDuration = new TimeSpan(hours, minutes, seconds);
        _remainingTime = _initialDuration;

        OnPropertyChanged(nameof(DisplayTimeText));
        OnPropertyChanged(nameof(RunStateText));
        OnPropertyChanged(nameof(IsStartEnabled));
        _startCommand.RaiseCanExecuteChanged();
        RequestSave();
    }

    private void ApplyInputsFromDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            _hoursInput = string.Empty;
            _minutesInput = string.Empty;
            _secondsInput = string.Empty;
        }
        else
        {
            _hoursInput = FormatInputComponent(Math.Clamp(Math.Max(0, (int)duration.TotalHours), 0, 60));
            _minutesInput = FormatInputComponent(Math.Clamp(duration.Minutes, 0, 60));
            _secondsInput = FormatInputComponent(Math.Clamp(duration.Seconds, 0, 60));
        }

        OnPropertyChanged(nameof(HoursInput));
        OnPropertyChanged(nameof(MinutesInput));
        OnPropertyChanged(nameof(SecondsInput));
    }

    private void RequestSave()
    {
        if (_isApplyingState)
        {
            return;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int ParsePositiveValue(string? value)
    {
        return int.TryParse(value, out var result) && result >= 0 ? result : 0;
    }

    private static string FormatInputComponent(int value)
    {
        return value <= 0 ? string.Empty : value.ToString();
    }

    private static string NormalizeTimeInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
        {
            return string.Empty;
        }

        if (!int.TryParse(digitsOnly, out var parsedValue))
        {
            return "60";
        }

        return Math.Clamp(parsedValue, 0, 60).ToString();
    }
}


