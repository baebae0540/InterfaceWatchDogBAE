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
    private FileSystemWatcher? _configWatcher;
    private DateTime _lastConfigWriteUtc;

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

            StartConfigWatcher();
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
        _configWatcher?.Dispose();
        _engine?.Stop();
        _engine?.Dispose();
    }

    // config.json 변경 감지 — 트레이 앱에서 설정을 저장하면 서비스도 재시작 없이 즉시 반영
    private void StartConfigWatcher()
    {
        _lastConfigWriteUtc = File.GetLastWriteTimeUtc(ConfigManager.ConfigFilePath);

        _configWatcher = new FileSystemWatcher(
            Path.GetDirectoryName(ConfigManager.ConfigFilePath)!,
            Path.GetFileName(ConfigManager.ConfigFilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _configWatcher.Changed += OnConfigFileChanged;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // 동일 저장에 대해 여러 번 발생하는 중복 이벤트 무시
            var writeTime = File.GetLastWriteTimeUtc(ConfigManager.ConfigFilePath);
            if (writeTime == _lastConfigWriteUtc) return;
            _lastConfigWriteUtc = writeTime;

            Thread.Sleep(200); // 파일 쓰기 완료 대기
            _engine?.ReloadConfig(ConfigManager.Load());
            _log?.Info(SvcName, "설정 변경 감지 — 감시 엔진 재적용됨");
        }
        catch
        {
            // 파일이 잠시 잠겨 있는 경우 등은 다음 변경 이벤트에서 재시도
        }
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
