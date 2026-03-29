using System.Collections.ObjectModel;
using ProductivityTimer.Infrastructure;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly ITimeProvider _timeProvider;
    private readonly IStorageService _storageService;
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase _currentViewModel = null!;
    private string _currentSectionTitle = string.Empty;
    private string _currentSectionDescription = string.Empty;
    private bool _isInitializing;

    public MainViewModel(
        ITimeProvider timeProvider,
        IStorageService storageService,
        DashboardViewModel dashboardViewModel,
        PomodoroViewModel pomodoroViewModel,
        TimerViewModel timerViewModel,
        StopwatchViewModel stopwatchViewModel,
        StatisticsViewModel statisticsViewModel)
    {
        _timeProvider = timeProvider;
        _storageService = storageService;

        DashboardViewModel = dashboardViewModel;
        PomodoroViewModel = pomodoroViewModel;
        TimerViewModel = timerViewModel;
        StopwatchViewModel = stopwatchViewModel;
        StatisticsViewModel = statisticsViewModel;

        DashboardViewModel.StateChanged += OnStateChanged;
        PomodoroViewModel.StateChanged += OnStateChanged;
        TimerViewModel.StateChanged += OnStateChanged;
        StopwatchViewModel.StateChanged += OnStateChanged;

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new(AppSection.Dashboard, "Главный экран", string.Empty, DashboardViewModel),
            new(AppSection.Pomodoro, "Pomodoro", string.Empty, PomodoroViewModel),
            new(AppSection.Timer, "Таймер", string.Empty, TimerViewModel),
            new(AppSection.Stopwatch, "Секундомер", string.Empty, StopwatchViewModel),
            new(AppSection.Statistics, "Статистика", string.Empty, StatisticsViewModel)
        };

        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public DashboardViewModel DashboardViewModel { get; }

    public PomodoroViewModel PomodoroViewModel { get; }

    public TimerViewModel TimerViewModel { get; }

    public StopwatchViewModel StopwatchViewModel { get; }

    public StatisticsViewModel StatisticsViewModel { get; }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value) && value is not null)
            {
                CurrentViewModel = value.ViewModel;
                CurrentSectionTitle = value.Title;
                CurrentSectionDescription = value.Description;
            }
        }
    }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        private set => SetProperty(ref _currentSectionTitle, value);
    }

    public string CurrentSectionDescription
    {
        get => _currentSectionDescription;
        private set => SetProperty(ref _currentSectionDescription, value);
    }

    public void Initialize()
    {
        _isInitializing = true;

        try
        {
            var appState = _storageService.Load(_timeProvider.Today);

            PomodoroViewModel.ApplyState(appState);
            DashboardViewModel.ApplyState(appState);
            TimerViewModel.ApplyState(appState);
            StopwatchViewModel.ApplyState(appState);
            StatisticsViewModel.ApplyState(appState);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public void PauseAndSaveOnClose()
    {
        PomodoroViewModel.PauseForAppClosing();
        TimerViewModel.PauseForAppClosing();
        StopwatchViewModel.PauseForAppClosing();
        SaveState();
    }

    public void SaveState()
    {
        var appState = AppState.CreateDefault(_timeProvider.Today);

        DashboardViewModel.UpdateState(appState);
        PomodoroViewModel.UpdateState(appState);
        TimerViewModel.UpdateState(appState);
        StopwatchViewModel.UpdateState(appState);
        StatisticsViewModel.UpdateState(appState);

        appState.LastUsedDate = _timeProvider.Today;
        _storageService.Save(appState);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        SaveState();
    }
}
