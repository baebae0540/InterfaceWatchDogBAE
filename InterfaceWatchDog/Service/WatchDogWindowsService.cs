using System.ServiceProcess;
using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;

namespace InterfaceWatchDog.Service;

public class WatchDogWindowsService : ServiceBase
{
    public const string SvcName = "InterfaceWatchDog";
    public const string SvcDisplayName = "Interface WatchDog Service";

    private WatchDogEngine? _engine;
    private LogWriter? _log;

    public WatchDogWindowsService()
    {
        base.ServiceName = SvcName;
        CanStop = true;
        CanPauseAndContinue = false;
        AutoLog = false;
    }

    protected override void OnStart(string[] args)
    {
        try
        {
            _log = new LogWriter();

            if (ConfigManager.IsFirstRun())
            {
                _log.Warn(SvcName, "config.json 없음 — 앱을 실행하여 설정을 완료한 뒤 서비스를 재시작하세요.");
                System.Diagnostics.EventLog.WriteEntry(SvcName,
                    "설정 파일(config.json)이 없습니다. 앱을 실행하여 설정 후 서비스를 재시작하세요.",
                    System.Diagnostics.EventLogEntryType.Warning);
                // 서비스는 Running 상태로 유지하되 감시는 시작하지 않음
                // 설정 후 서비스 재시작으로 감시 시작
                return;
            }

            var config = ConfigManager.Load();
            _engine = new WatchDogEngine(config, _log);
            _engine.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.EventLog.WriteEntry(SvcName,
                $"서비스 시작 실패: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Error);
            Stop();
        }
    }

    protected override void OnStop()
    {
        _engine?.Stop();
        _engine?.Dispose();
    }

    public static void Install()
    {
        var exePath = Environment.ProcessPath ?? "";
        RunSc($"create {SvcName} binPath= \"{exePath}\" start= auto DisplayName= \"{SvcDisplayName}\"");
        RunSc($"description {SvcName} \"ERWEKA Export Manager / TabmachineIF 프로세스 감시 및 자동 재시작\"");
        RunSc($"start {SvcName}");
    }

    public static void Uninstall()
    {
        RunSc($"stop {SvcName}");
        RunSc($"delete {SvcName}");
    }

    private static void RunSc(string args)
    {
        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        });
        p?.WaitForExit(5000);
    }
}
