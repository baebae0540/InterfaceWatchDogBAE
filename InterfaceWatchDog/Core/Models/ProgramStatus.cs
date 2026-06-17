namespace InterfaceWatchDog.Core.Models;

public enum HealthStatus
{
    Unknown,
    Healthy,
    Warning,
    Restarting,
    Failed,
    Disabled
}

public class ProgramStatus
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    public bool IsRunning { get; set; }
    public int RestartCount { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime? LastSeenAlive { get; set; }
    public DateTime? LastRestartTime { get; set; }
    public string StatusMessage { get; set; } = "";
    public int AlarmCount { get; set; }

    public string StatusText => Status switch
    {
        HealthStatus.Healthy => "정상",
        HealthStatus.Warning => "경고",
        HealthStatus.Restarting => "재시작 중",
        HealthStatus.Failed => "복구 실패",
        HealthStatus.Disabled => "감시 안함",
        _ => "확인 중"
    };

    public Color StatusColor => Status switch
    {
        HealthStatus.Healthy => Color.FromArgb(76, 175, 80),
        HealthStatus.Warning => Color.FromArgb(255, 152, 0),
        HealthStatus.Restarting => Color.FromArgb(33, 150, 243),
        HealthStatus.Failed => Color.FromArgb(244, 67, 54),
        HealthStatus.Disabled => Color.FromArgb(189, 193, 204),
        _ => Color.FromArgb(158, 158, 158)
    };
}

public class FileActivityStatus
{
    public bool IsFolderConfigured { get; set; }
    public int FileCount { get; set; }
    public DateTime? LastFileCreated { get; set; }
    public bool IsIdleWarning { get; set; }
    public bool IsBacklogWarning { get; set; }
    public string StatusMessage { get; set; } = "";
}
