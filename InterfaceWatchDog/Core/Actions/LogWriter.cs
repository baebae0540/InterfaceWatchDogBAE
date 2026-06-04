using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Actions;

public class LogWriter
{
    private static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "InterfaceWatchDog", "Logs");

    private readonly object _lock = new();

    public static string LogDirectoryPath => LogDirectory;

    public event Action<LogEntry>? LogGenerated;

    public void Info(string source, string message) => Write(LogLevel.Info, source, message);
    public void Warn(string source, string message) => Write(LogLevel.Warning, source, message);
    public void Error(string source, string message) => Write(LogLevel.Error, source, message);

    private void Write(LogLevel level, string source, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Source = source,
            Message = message
        };

        WriteToFile(entry);
        LogGenerated?.Invoke(entry);
    }

    private void WriteToFile(LogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var logFile = Path.Combine(LogDirectory, $"watchdog_{DateTime.Now:yyyy-MM-dd}.log");

            lock (_lock)
            {
                File.AppendAllText(logFile, entry.ToString() + Environment.NewLine);
            }
        }
        catch { /* 로그 실패는 무시 */ }
    }

    public List<LogEntry> ReadTodayLogs()
    {
        var entries = new List<LogEntry>();
        var logFile = Path.Combine(LogDirectory, $"watchdog_{DateTime.Now:yyyy-MM-dd}.log");

        if (!File.Exists(logFile)) return entries;

        try
        {
            lock (_lock)
            {
                var lines = File.ReadAllLines(logFile);
                foreach (var line in lines)
                {
                    var parsed = ParseLogLine(line);
                    if (parsed != null) entries.Add(parsed);
                }
            }
        }
        catch { /* 읽기 실패 무시 */ }

        return entries;
    }

    public List<string> GetAvailableLogDates()
    {
        if (!Directory.Exists(LogDirectory)) return [];

        return Directory.GetFiles(LogDirectory, "watchdog_*.log")
                        .Select(f => Path.GetFileNameWithoutExtension(f).Replace("watchdog_", ""))
                        .OrderByDescending(d => d)
                        .ToList();
    }

    public List<LogEntry> ReadLogsByDate(string date)
    {
        var entries = new List<LogEntry>();
        var logFile = Path.Combine(LogDirectory, $"watchdog_{date}.log");

        if (!File.Exists(logFile)) return entries;

        try
        {
            lock (_lock)
            {
                var lines = File.ReadAllLines(logFile);
                foreach (var line in lines)
                {
                    var parsed = ParseLogLine(line);
                    if (parsed != null) entries.Add(parsed);
                }
            }
        }
        catch { }

        return entries;
    }

    private static LogEntry? ParseLogLine(string line)
    {
        // 형식: [2026-06-04 09:12:33] [INFO ] [Source] Message
        if (string.IsNullOrWhiteSpace(line)) return null;

        try
        {
            var timestampEnd = line.IndexOf(']');
            if (timestampEnd < 0) return null;

            var timestamp = DateTime.Parse(line[1..timestampEnd]);

            var levelStart = line.IndexOf('[', timestampEnd + 1) + 1;
            var levelEnd = line.IndexOf(']', levelStart);
            var levelStr = line[levelStart..levelEnd].Trim();

            var sourceStart = line.IndexOf('[', levelEnd + 1) + 1;
            var sourceEnd = line.IndexOf(']', sourceStart);
            var source = line[sourceStart..sourceEnd];

            var message = line[(sourceEnd + 2)..].Trim();

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = levelStr switch
                {
                    "WARN" => LogLevel.Warning,
                    "ERROR" => LogLevel.Error,
                    _ => LogLevel.Info
                },
                Source = source,
                Message = message
            };
        }
        catch
        {
            return new LogEntry { Message = line };
        }
    }
}
