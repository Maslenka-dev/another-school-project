using ProductivityTimer.ViewModels;

namespace ProductivityTimer.Infrastructure;

public sealed class NavigationItem
{
    public NavigationItem(AppSection section, string title, string description, ViewModelBase viewModel)
    {
        Section = section;
        Title = title;
        Description = description;
        ViewModel = viewModel;
    }

    public AppSection Section { get; }

    public string Title { get; }

    public string Description { get; }

    public ViewModelBase ViewModel { get; }
}
