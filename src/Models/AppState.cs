namespace ProductivityTimer.Models;

public sealed class AppState
{
    private const string DefaultTaskListTitle = "Мои задачи";
    private const string DefaultTaskTitle = "Новая задача";

    public string TaskListTitle { get; set; } = DefaultTaskListTitle;

    public List<TaskModel> Tasks { get; set; } = new();

    public PomodoroState Pomodoro { get; set; } = new();

    public TimerState Timer { get; set; } = new();

    public StopwatchState Stopwatch { get; set; } = new();

    public StatisticsModel Statistics { get; set; } = new();

    public DateTime LastUsedDate { get; set; }

    public static AppState CreateDefault(DateTime currentDate)
    {
        var normalizedDate = currentDate.Date;

        return new AppState
        {
            TaskListTitle = DefaultTaskListTitle,
            Pomodoro = new PomodoroState
            {
                ModeId = "25/5",
                CurrentPhase = "Work",
                RemainingTime = TimeSpan.FromMinutes(25),
                CycleNumber = 1,
                IsRunning = false
            },
            Timer = new TimerState
            {
                InitialDuration = TimeSpan.Zero,
                RemainingTime = TimeSpan.Zero,
                IsRunning = false
            },
            Stopwatch = new StopwatchState
            {
                Elapsed = TimeSpan.Zero,
                IsRunning = false
            },
            Statistics = new StatisticsModel
            {
                StatisticsDate = normalizedDate,
                CompletedPomodoroCount = 0,
                TotalWorkTime = TimeSpan.Zero
            },
            LastUsedDate = normalizedDate
        };
    }

    public static AppState Sanitize(AppState? state, DateTime currentDate)
    {
        var safeState = CreateDefault(currentDate);
        if (state is null)
        {
            return safeState;
        }

        safeState.TaskListTitle = string.IsNullOrWhiteSpace(state.TaskListTitle)
            ? DefaultTaskListTitle
            : state.TaskListTitle.Trim();

        safeState.Tasks = NormalizeTasks(state.Tasks);

        if (state.Pomodoro is not null)
        {
            safeState.Pomodoro.ModeId = string.IsNullOrWhiteSpace(state.Pomodoro.ModeId)
                ? safeState.Pomodoro.ModeId
                : state.Pomodoro.ModeId;
            safeState.Pomodoro.CurrentPhase = state.Pomodoro.CurrentPhase == "Break" ? "Break" : "Work";
            safeState.Pomodoro.RemainingTime = state.Pomodoro.RemainingTime > TimeSpan.Zero
                ? state.Pomodoro.RemainingTime
                : safeState.Pomodoro.RemainingTime;
            safeState.Pomodoro.CycleNumber = state.Pomodoro.CycleNumber > 0 ? state.Pomodoro.CycleNumber : 1;
            safeState.Pomodoro.IsRunning = state.Pomodoro.IsRunning;
        }

        if (state.Timer is not null)
        {
            var initialDuration = state.Timer.InitialDuration < TimeSpan.Zero
                ? TimeSpan.Zero
                : state.Timer.InitialDuration;
            var remainingTime = state.Timer.RemainingTime < TimeSpan.Zero
                ? TimeSpan.Zero
                : state.Timer.RemainingTime;

            if (initialDuration > TimeSpan.Zero && remainingTime > initialDuration)
            {
                remainingTime = initialDuration;
            }

            safeState.Timer.InitialDuration = initialDuration;
            safeState.Timer.RemainingTime = remainingTime;
            safeState.Timer.IsRunning = state.Timer.IsRunning && remainingTime > TimeSpan.Zero;
        }

        if (state.Stopwatch is not null)
        {
            var elapsed = state.Stopwatch.Elapsed < TimeSpan.Zero
                ? TimeSpan.Zero
                : state.Stopwatch.Elapsed;

            safeState.Stopwatch.Elapsed = elapsed;
            safeState.Stopwatch.IsRunning = state.Stopwatch.IsRunning;
        }

        if (state.Statistics is not null)
        {
            safeState.Statistics.StatisticsDate = state.Statistics.StatisticsDate == default
                ? currentDate.Date
                : state.Statistics.StatisticsDate.Date;
            safeState.Statistics.CompletedPomodoroCount = Math.Max(0, state.Statistics.CompletedPomodoroCount);
            safeState.Statistics.TotalWorkTime = state.Statistics.TotalWorkTime < TimeSpan.Zero
                ? TimeSpan.Zero
                : state.Statistics.TotalWorkTime;
        }

        safeState.LastUsedDate = state.LastUsedDate == default
            ? currentDate.Date
            : state.LastUsedDate.Date;

        return safeState;
    }

    private static List<TaskModel> NormalizeTasks(List<TaskModel>? tasks)
    {
        if (tasks is null || tasks.Count == 0)
        {
            return new List<TaskModel>();
        }

        return tasks
            .Where(task => task is not null)
            .OrderBy(task => task.Order)
            .Select((task, index) => new TaskModel
            {
                Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id,
                Title = string.IsNullOrWhiteSpace(task.Title) ? DefaultTaskTitle : task.Title.Trim(),
                IsCompleted = task.IsCompleted,
                Order = index
            })
            .ToList();
    }
}
