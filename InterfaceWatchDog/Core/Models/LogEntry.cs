namespace InterfaceWatchDog.Core.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; }
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";

    public string LevelText => Level switch
    {
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        _ => "INFO"
    };

    public Color LevelColor => Level switch
    {
        LogLevel.Warning => Color.FromArgb(255, 152, 0),
        LogLevel.Error => Color.FromArgb(244, 67, 54),
        _ => Color.FromArgb(33, 150, 243)
    };

    public override string ToString()
        => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{LevelText,-5}] [{Source}] {Message}";
}
