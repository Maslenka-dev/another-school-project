using System.Text.Json.Serialization;
using ProductivityTimer.Infrastructure;

namespace ProductivityTimer.Models;

public sealed class TaskModel : ObservableObject
{
    private string _title = string.Empty;
    private bool _isCompleted;
    private int _order;
    private bool _isEditing;
    private string _editSnapshotTitle = string.Empty;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public int Order
    {
        get => _order;
        set => SetProperty(ref _order, value);
    }

    [JsonIgnore]
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    [JsonIgnore]
    public string EditSnapshotTitle
    {
        get => _editSnapshotTitle;
        set => SetProperty(ref _editSnapshotTitle, value);
    }

    public TaskModel Clone()
    {
        return new TaskModel
        {
            Id = Id,
            Title = Title,
            IsCompleted = IsCompleted,
            Order = Order
        };
    }
}
