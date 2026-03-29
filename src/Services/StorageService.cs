using System.IO;
using System.Text;
using System.Text.Json;
using ProductivityTimer.Models;

namespace ProductivityTimer.Services;

public sealed class StorageService : IStorageService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly string _filePath;

    public StorageService()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = AppContext.BaseDirectory;
        }

        _filePath = Path.Combine(baseDirectory, "ProductivityTimer", "appstate.json");
    }

    public AppState Load(DateTime currentDate)
    {
        try
        {
            EnsureStorageDirectoryExists();

            if (!File.Exists(_filePath))
            {
                return CreateAndPersistDefaultState(currentDate);
            }

            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateAndPersistDefaultState(currentDate);
            }

            var loadedState = JsonSerializer.Deserialize<AppState>(json, _serializerOptions);
            var safeState = AppState.Sanitize(loadedState, currentDate);
            PersistSafely(safeState);
            return safeState;
        }
        catch
        {
            var safeState = AppState.CreateDefault(currentDate);
            PersistSafely(safeState);
            return safeState;
        }
    }

    public void Save(AppState state)
    {
        var statisticsDate = state.Statistics is not null ? state.Statistics.StatisticsDate : default;
        var safeDate = state.LastUsedDate != default
            ? state.LastUsedDate
            : statisticsDate != default
                ? statisticsDate
                : DateTime.Today;

        var safeState = AppState.Sanitize(state, safeDate);
        PersistSafely(safeState);
    }

    private AppState CreateAndPersistDefaultState(DateTime currentDate)
    {
        var safeState = AppState.CreateDefault(currentDate);
        PersistSafely(safeState);
        return safeState;
    }

    private void PersistSafely(AppState state)
    {
        var tempFilePath = _filePath + ".tmp";

        try
        {
            EnsureStorageDirectoryExists();

            var json = JsonSerializer.Serialize(state, _serializerOptions);
            File.WriteAllText(tempFilePath, json, Encoding.UTF8);

            if (File.Exists(_filePath))
            {
                File.Replace(tempFilePath, _filePath, null, true);
            }
            else
            {
                File.Move(tempFilePath, _filePath);
            }
        }
        catch
        {
            TryDeleteTempFile(tempFilePath);
        }
    }

    private void EnsureStorageDirectoryExists()
    {
        var directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static void TryDeleteTempFile(string tempFilePath)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch
        {
            // Игнорируем вторичную ошибку очистки временного файла.
        }
    }
}
