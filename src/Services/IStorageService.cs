using ProductivityTimer.Models;

namespace ProductivityTimer.Services;

public interface IStorageService
{
    AppState Load(DateTime currentDate);

    void Save(AppState state);
}
