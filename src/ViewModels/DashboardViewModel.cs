using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using ProductivityTimer.Infrastructure;
using ProductivityTimer.Models;
using ProductivityTimer.Services;

namespace ProductivityTimer.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private const string DefaultTaskListTitle = "Мои задачи";

    private readonly ITimeProvider _timeProvider;
    private readonly ITimerService _clockService;
    private readonly IPomodoroService _pomodoroService;
    private readonly RelayCommand _addTaskCommand;
    private readonly RelayCommand _deleteTaskCommand;
    private readonly RelayCommand _moveUpTaskCommand;
    private readonly RelayCommand _moveDownTaskCommand;
    private readonly RelayCommand _editTaskCommand;

    private string _taskListTitle = DefaultTaskListTitle;
    private string _currentTimeText = "00:00:00";
    private string _currentDateText = string.Empty;
    private string _pomodoroPhaseText = "Учёба";
    private string _pomodoroRemainingTimeText = "00:00:00";
    private string _pomodoroCycleText = "Цикл 1";
    private string _pomodoroStatusText = string.Empty;
    private bool _isApplyingState;

    public DashboardViewModel(ITimeProvider timeProvider, ITimerService clockService, IPomodoroService pomodoroService)
    {
        _timeProvider = timeProvider;
        _clockService = clockService;
        _pomodoroService = pomodoroService;

        Tasks = new ObservableCollection<TaskModel>();
        Tasks.CollectionChanged += OnTasksCollectionChanged;

        _addTaskCommand = new RelayCommand(_ => AddTask());
        _deleteTaskCommand = new RelayCommand(task => DeleteTask(task as TaskModel));
        _moveUpTaskCommand = new RelayCommand(task => MoveTask(task as TaskModel, -1));
        _moveDownTaskCommand = new RelayCommand(task => MoveTask(task as TaskModel, 1));
        _editTaskCommand = new RelayCommand(task => ToggleTaskEditing(task as TaskModel));

        AddTaskCommand = _addTaskCommand;
        DeleteTaskCommand = _deleteTaskCommand;
        MoveUpTaskCommand = _moveUpTaskCommand;
        MoveDownTaskCommand = _moveDownTaskCommand;
        EditTaskCommand = _editTaskCommand;

        _clockService.Interval = TimeSpan.FromSeconds(1);
        _clockService.Tick += OnClockTick;
        _pomodoroService.StateChanged += OnPomodoroStateChanged;

        UpdateClock();
        UpdatePomodoroStatus(_pomodoroService.CurrentState);
        _clockService.Start();
    }

    public event EventHandler? StateChanged;

    public ObservableCollection<TaskModel> Tasks { get; }

    public string TaskListTitle
    {
        get => _taskListTitle;
        set
        {
            if (SetProperty(ref _taskListTitle, value))
            {
                RequestSave();
            }
        }
    }

    public string CurrentTimeText
    {
        get => _currentTimeText;
        private set => SetProperty(ref _currentTimeText, value);
    }

    public string CurrentDateText
    {
        get => _currentDateText;
        private set => SetProperty(ref _currentDateText, value);
    }

    public string PomodoroPhaseText
    {
        get => _pomodoroPhaseText;
        private set => SetProperty(ref _pomodoroPhaseText, value);
    }

    public string PomodoroRemainingTimeText
    {
        get => _pomodoroRemainingTimeText;
        private set => SetProperty(ref _pomodoroRemainingTimeText, value);
    }

    public string PomodoroCycleText
    {
        get => _pomodoroCycleText;
        private set => SetProperty(ref _pomodoroCycleText, value);
    }

    public string PomodoroStatusText
    {
        get => _pomodoroStatusText;
        private set => SetProperty(ref _pomodoroStatusText, value);
    }

    public RelayCommand AddTaskCommand { get; }

    public RelayCommand DeleteTaskCommand { get; }

    public RelayCommand MoveUpTaskCommand { get; }

    public RelayCommand MoveDownTaskCommand { get; }

    public RelayCommand EditTaskCommand { get; }

    public void ApplyState(AppState appState)
    {
        _isApplyingState = true;

        try
        {
            foreach (var existingTask in Tasks)
            {
                UnsubscribeFromTask(existingTask);
            }

            Tasks.Clear();
            TaskListTitle = appState.TaskListTitle;

            foreach (var task in appState.Tasks.OrderBy(task => task.Order))
            {
                Tasks.Add(task.Clone());
            }

            SynchronizeTaskOrder();
            UpdateClock();
            UpdatePomodoroStatus(_pomodoroService.CurrentState);
        }
        finally
        {
            _isApplyingState = false;
        }
    }

    public void UpdateState(AppState appState)
    {
        appState.TaskListTitle = string.IsNullOrWhiteSpace(TaskListTitle)
            ? DefaultTaskListTitle
            : TaskListTitle.Trim();

        var persistedTasks = new List<TaskModel>();
        foreach (var task in Tasks)
        {
            var persistedTitle = ResolvePersistedTaskTitle(task);
            if (persistedTitle is null)
            {
                continue;
            }

            persistedTasks.Add(new TaskModel
            {
                Id = task.Id,
                Title = persistedTitle,
                IsCompleted = task.IsCompleted,
                Order = persistedTasks.Count
            });
        }

        appState.Tasks = persistedTasks;
    }

    private void AddTask()
    {
        SetEditingStateForSingleTask(null);

        var task = new TaskModel
        {
            Id = Guid.NewGuid(),
            Title = string.Empty,
            IsCompleted = false,
            Order = Tasks.Count,
            IsEditing = true,
            EditSnapshotTitle = string.Empty
        };

        Tasks.Add(task);
        SynchronizeTaskOrder();
        RequestSave();
    }

    private void DeleteTask(TaskModel? task)
    {
        if (task is null)
        {
            return;
        }

        if (!Tasks.Remove(task))
        {
            return;
        }

        SynchronizeTaskOrder();
        RequestSave();
    }

    private void MoveTask(TaskModel? task, int direction)
    {
        if (task is null)
        {
            return;
        }

        var currentIndex = Tasks.IndexOf(task);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex + direction;
        if (targetIndex < 0 || targetIndex >= Tasks.Count)
        {
            return;
        }

        Tasks.Move(currentIndex, targetIndex);
        SynchronizeTaskOrder();
        RequestSave();
    }

    private void ToggleTaskEditing(TaskModel? task)
    {
        if (task is null)
        {
            return;
        }

        if (task.IsEditing)
        {
            CommitTaskEditing(task);
            return;
        }

        SetEditingStateForSingleTask(task);
    }

    private void SetEditingStateForSingleTask(TaskModel? activeTask)
    {
        var tasksToRemove = new List<TaskModel>();
        var committedAnyChanges = false;

        foreach (var task in Tasks)
        {
            if (task == activeTask)
            {
                if (!task.IsEditing)
                {
                    task.EditSnapshotTitle = task.Title;
                    task.IsEditing = true;
                }

                continue;
            }

            if (!task.IsEditing)
            {
                continue;
            }

            committedAnyChanges |= ApplyTaskEdit(task, tasksToRemove);
        }

        if (tasksToRemove.Count > 0)
        {
            foreach (var task in tasksToRemove)
            {
                Tasks.Remove(task);
            }

            SynchronizeTaskOrder();
            committedAnyChanges = true;
        }

        if (committedAnyChanges)
        {
            RequestSave();
        }
    }

    private void CommitTaskEditing(TaskModel task)
    {
        var tasksToRemove = new List<TaskModel>();
        var committedChanges = ApplyTaskEdit(task, tasksToRemove);

        if (tasksToRemove.Count > 0)
        {
            foreach (var item in tasksToRemove)
            {
                Tasks.Remove(item);
            }

            SynchronizeTaskOrder();
            committedChanges = true;
        }

        if (committedChanges)
        {
            RequestSave();
        }
    }

    private bool ApplyTaskEdit(TaskModel task, ICollection<TaskModel> tasksToRemove)
    {
        task.IsEditing = false;

        var currentTitle = NormalizeTaskTitle(task.Title);
        var snapshotTitle = NormalizeTaskTitle(task.EditSnapshotTitle);
        task.EditSnapshotTitle = string.Empty;

        if (currentTitle is not null)
        {
            if (task.Title == currentTitle)
            {
                return false;
            }

            task.Title = currentTitle;
            return true;
        }

        if (snapshotTitle is not null)
        {
            if (task.Title == snapshotTitle)
            {
                return false;
            }

            task.Title = snapshotTitle;
            return true;
        }

        tasksToRemove.Add(task);
        return true;
    }

    private void SynchronizeTaskOrder()
    {
        for (var index = 0; index < Tasks.Count; index++)
        {
            Tasks[index].Order = index;
        }
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TaskModel task in e.OldItems)
            {
                UnsubscribeFromTask(task);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TaskModel task in e.NewItems)
            {
                SubscribeToTask(task);
            }
        }
    }

    private void SubscribeToTask(TaskModel task)
    {
        task.PropertyChanged += OnTaskPropertyChanged;
    }

    private void UnsubscribeFromTask(TaskModel task)
    {
        task.PropertyChanged -= OnTaskPropertyChanged;
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isApplyingState)
        {
            return;
        }

        if (sender is not TaskModel task)
        {
            return;
        }

        if (e.PropertyName == nameof(TaskModel.Title) && task.IsEditing)
        {
            RequestSave();
            return;
        }

        if (e.PropertyName == nameof(TaskModel.IsCompleted))
        {
            RequestSave();
        }
    }

    private void RequestSave()
    {
        if (_isApplyingState)
        {
            return;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        if (_pomodoroService.RefreshStatisticsForToday())
        {
            RequestSave();
        }

        UpdateClock();
    }

    private void OnPomodoroStateChanged(object? sender, EventArgs e)
    {
        UpdatePomodoroStatus(_pomodoroService.CurrentState);
    }

    private void UpdateClock()
    {
        var now = _timeProvider.Now;
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        CurrentTimeText = now.ToString("HH:mm:ss", culture);
        CurrentDateText = now.ToString("dddd, dd MMMM yyyy", culture);
    }

    private void UpdatePomodoroStatus(PomodoroState state)
    {
        var phaseText = state.CurrentPhase == "Break" ? "Отдых" : "Учёба";
        var remainingTimeText = state.RemainingTime.ToString(@"hh\:mm\:ss");

        PomodoroPhaseText = phaseText;
        PomodoroRemainingTimeText = remainingTimeText;
        PomodoroCycleText = $"Цикл {state.CycleNumber}";
        PomodoroStatusText = $"{phaseText} • {remainingTimeText} • цикл {state.CycleNumber}";
    }

    private static string? ResolvePersistedTaskTitle(TaskModel task)
    {
        var currentTitle = NormalizeTaskTitle(task.Title);
        if (currentTitle is not null)
        {
            return currentTitle;
        }

        return NormalizeTaskTitle(task.EditSnapshotTitle);
    }

    private static string? NormalizeTaskTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return title.Trim();
    }
}
