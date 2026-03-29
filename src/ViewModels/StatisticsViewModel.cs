using System.Globalization;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class StatisticsViewModel : ViewModelBase
{
    private readonly IPomodoroService _pomodoroService;
    private DateTime _statisticsDate;
    private int _completedPomodoroCount;
    private TimeSpan _totalWorkTime = TimeSpan.Zero;

    public StatisticsViewModel(IPomodoroService pomodoroService)
    {
        _pomodoroService = pomodoroService;
        _pomodoroService.StatisticsChanged += OnStatisticsChanged;

        SyncFromService();
    }

    public DateTime StatisticsDate
    {
        get => _statisticsDate;
        private set
        {
            if (SetProperty(ref _statisticsDate, value))
            {
                OnPropertyChanged(nameof(StatisticsDateText));
            }
        }
    }

    public int CompletedPomodoroCount
    {
        get => _completedPomodoroCount;
        private set => SetProperty(ref _completedPomodoroCount, value);
    }

    public TimeSpan TotalWorkTime
    {
        get => _totalWorkTime;
        private set
        {
            if (SetProperty(ref _totalWorkTime, value))
            {
                OnPropertyChanged(nameof(WorkedTimeTodayText));
            }
        }
    }

    public string StatisticsDateText => StatisticsDate == default
        ? "Не задано"
        : StatisticsDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));

    public string WorkedTimeTodayText
    {
        get
        {
            var totalMinutes = (int)Math.Floor(TotalWorkTime.TotalMinutes);
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;

            if (hours <= 0)
            {
                return $"{minutes} мин";
            }

            return $"{hours} ч {minutes} мин";
        }
    }

    public void ApplyState(AppState appState)
    {
        SyncFromService();
    }

    public void UpdateState(AppState appState)
    {
        var statistics = _pomodoroService.CurrentStatistics;

        appState.Statistics = new StatisticsModel
        {
            StatisticsDate = statistics.StatisticsDate,
            CompletedPomodoroCount = statistics.CompletedPomodoroCount,
            TotalWorkTime = statistics.TotalWorkTime
        };
    }

    private void OnStatisticsChanged(object? sender, EventArgs e)
    {
        SyncFromService();
    }

    private void SyncFromService()
    {
        var statistics = _pomodoroService.CurrentStatistics;

        StatisticsDate = statistics.StatisticsDate;
        CompletedPomodoroCount = statistics.CompletedPomodoroCount;
        TotalWorkTime = statistics.TotalWorkTime;
    }
}
