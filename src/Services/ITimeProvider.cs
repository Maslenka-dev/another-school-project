namespace ProductivityTimer.Services;

public interface ITimeProvider
{
    DateTime Now { get; }

    DateTime Today { get; }
}
