using System.Text.Json.Serialization;

namespace ProductivityTimer.Models;

public sealed class StatisticsModel
{
    public DateTime StatisticsDate { get; set; }

    [JsonPropertyName("CompletedPomodorosToday")]
    public int CompletedPomodoroCount { get; set; }

    [JsonPropertyName("WorkedTimeToday")]
    public TimeSpan TotalWorkTime { get; set; } = TimeSpan.Zero;
}
