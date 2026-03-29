using ProductivityTimer.Services;
using ProductivityTimer.ViewModels;

namespace ProductivityTimer;

public partial class App : System.Windows.Application
{
    private INotificationService? _notificationService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var timeProvider = new MoscowTimeProvider();
            var storageService = new StorageService();
            _notificationService = new NotificationService();
            var pomodoroService = new PomodoroService(new TimerService(), _notificationService, timeProvider);

            var dashboardViewModel = new DashboardViewModel(timeProvider, new TimerService(), pomodoroService);
            var pomodoroViewModel = new PomodoroViewModel(pomodoroService);
            var timerViewModel = new TimerViewModel(new TimerService(), _notificationService);
            var stopwatchViewModel = new StopwatchViewModel(new TimerService());
            var statisticsViewModel = new StatisticsViewModel(pomodoroService);

            var mainViewModel = new MainViewModel(
                timeProvider,
                storageService,
                dashboardViewModel,
                pomodoroViewModel,
                timerViewModel,
                stopwatchViewModel,
                statisticsViewModel);

            mainViewModel.Initialize();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            if (_notificationService is IDisposable disposable)
            {
                disposable.Dispose();
                _notificationService = null;
            }

            System.Windows.MessageBox.Show(
                $"Не удалось запустить приложение. {exception.Message}",
                "Таймер продуктивности",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        if (_notificationService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}


