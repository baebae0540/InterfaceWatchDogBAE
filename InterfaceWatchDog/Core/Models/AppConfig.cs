using System.Text.Json.Serialization;

namespace InterfaceWatchDog.Core.Models;

public class AppConfig
{
    public ErwekaConfig Erweka { get; set; } = new();
    public TabmachineConfig TabmachineIF { get; set; } = new();
    public PdfFolderConfig PdfFolder { get; set; } = new();
    public bool DbConnectionVerified { get; set; }
}

public class ErwekaConfig
{
    public string DisplayName { get; set; } = "ERWEKA Export Manager";
    public string ProcessName { get; set; } = "javaw";
    public string Arguments { get; set; } = "";
    public int ProcessCheckSeconds { get; set; } = 30;
    public int Port { get; set; } = 0;
}

public class TabmachineConfig
{
    public string DisplayName { get; set; } = "TabmachineIF";
    public string ProcessName { get; set; } = "TabmachineIF";
    public string ExecutablePath { get; set; } = "";
    public string Arguments { get; set; } = "";
    public int MaxRestartAttempts { get; set; } = 3;
    public int RestartCooldownSeconds { get; set; } = 60;
    public int ProcessCheckSeconds { get; set; } = 30;

    [JsonIgnore]
    public bool CanRestart => !string.IsNullOrWhiteSpace(ExecutablePath) &&
                              File.Exists(ExecutablePath);
}

public class AlarmDbConfig
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Password { get; set; } = "";
    public string PlantCode { get; set; } = "";

    [JsonIgnore]
    public string ConnectionString =>
        $"Server={Server};Database={Database};User Id={UserId};Password={Password};TrustServerCertificate=True";

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server)
                             && !string.IsNullOrWhiteSpace(Database)
                             && !string.IsNullOrWhiteSpace(PlantCode);
}

public class PdfFolderConfig
{
    public bool Visible { get; set; }
    public string Path { get; set; } = "";
    public int MaxIdleMinutes { get; set; } = 30;
    public int MaxBacklogCount { get; set; } = 50;
    public int FileActivityCheckMinutes { get; set; } = 5;

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path);
}
