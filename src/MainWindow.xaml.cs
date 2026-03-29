using System.ComponentModel;
using ProductivityTimer.ViewModels;

namespace ProductivityTimer;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PauseAndSaveOnClose();
        }
    }
}
