using System.Text.Json.Serialization;

namespace InterfaceWatchDog.Core.Models;

public class AppConfig
{
    public ProgramConfig Erweka { get; set; } = new()
    {
        DisplayName = "ERWEKA Export Manager",
        ProcessName = "ExportManager",
        ExecutablePath = "",
        Arguments = "",
        MaxRestartAttempts = 3,
        RestartCooldownSeconds = 60
    };

    public ProgramConfig TabmachineIF { get; set; } = new()
    {
        DisplayName = "TabmachineIF",
        ProcessName = "TabmachineIF",
        ExecutablePath = "",
        Arguments = "",
        MaxRestartAttempts = 3,
        RestartCooldownSeconds = 60
    };

    public PdfFolderConfig PdfFolder { get; set; } = new();
}

public class ProgramConfig
{
    public string DisplayName { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string Arguments { get; set; } = "";
    public int MaxRestartAttempts { get; set; } = 3;
    public int RestartCooldownSeconds { get; set; } = 60;
    public int ProcessCheckSeconds { get; set; } = 30;

    [JsonIgnore]
    public bool CanRestart => !string.IsNullOrWhiteSpace(ExecutablePath) &&
                              File.Exists(ExecutablePath);
}

public class PdfFolderConfig
{
    public string Path { get; set; } = "";
    public int MaxIdleMinutes { get; set; } = 30;
    public int MaxBacklogCount { get; set; } = 50;
    public int FileActivityCheckMinutes { get; set; } = 5;

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path);
}
