using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core.Monitors;

namespace InterfaceWatchDog.Core;

public class WatchDogEngine : IDisposable
{
    private AppConfig _config;
    private readonly LogWriter _log;
    private readonly IProcessMonitor _processMonitor;
    private readonly IProcessRestarter _restarter;
    private readonly IFileActivityMonitor _fileMonitor;

    private System.Threading.Timer? _erwekaTimer;
    private System.Threading.Timer? _tabmachineTimer;
    private System.Threading.Timer? _fileTimer;
    private readonly object _fileCheckLock = new();
    private int _fileConfigVersion;

    private readonly Dictionary<string, RestartTracker> _trackers = new()
    {
        ["Erweka"]       = new RestartTracker(),
        ["TabmachineIF"] = new RestartTracker()
    };

    private ProgramStatus _erwekaStatus    = new() { Key = "Erweka",       DisplayName = "ERWEKA Export Manager" };
    private ProgramStatus _tabmachineStatus = new() { Key = "TabmachineIF", DisplayName = "TabmachineIF" };
    private FileActivityStatus _fileStatus  = new();

    public event Action<ProgramStatus>?     ProgramStatusChanged;
    public event Action<FileActivityStatus>? FileStatusChanged;

    // 이 인스턴스가 대화형 사용자 세션(트레이 앱)에서 실행 중인지 여부
    // (Windows 서비스는 세션 0에서 실행되며, 재시작은 이 값이 true인 인스턴스만
    //  전담한다 — 두 감시 대상 모두 재시작 후 사용자가 화면에서 설정/시작 버튼을
    //  눌러야 하므로, 세션 0에서 재시작하면 창이 보이지 않아 무용지물이 됨)
    private readonly bool _isInteractiveSession;

    // 프로덕션 생성자 — 구체 클래스를 직접 생성
    public WatchDogEngine(AppConfig config, LogWriter log, bool isInteractiveSession = true)
        : this(config, log, new ProcessMonitor(), new FileActivityMonitor(), new ProcessRestarter(), isInteractiveSession) { }

    // 테스트/DI 생성자 — 인터페이스 주입 (InternalsVisibleTo로 테스트 프로젝트에서 접근)
    internal WatchDogEngine(AppConfig config, LogWriter log,
        IProcessMonitor processMonitor, IFileActivityMonitor fileMonitor, IProcessRestarter restarter,
        bool isInteractiveSession = true)
    {
        _config         = config;
        _log            = log;
        _processMonitor = processMonitor;
        _fileMonitor    = fileMonitor;
        _restarter      = restarter;
        _isInteractiveSession = isInteractiveSession;
        _log.LogGenerated += _ => { };
    }

    public void Start()
    {
        _log.Info("Engine", "WatchDog 감시 시작");

        _erwekaTimer = new System.Threading.Timer(_ => CheckErweka(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(_config.Erweka.ProcessCheckSeconds));

        _tabmachineTimer = new System.Threading.Timer(_ => CheckTabmachine(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(_config.TabmachineIF.ProcessCheckSeconds));

        _fileTimer = new System.Threading.Timer(_ => CheckFileActivity(), null,
            TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(_config.PdfFolder.FileActivityCheckMinutes));
    }

    public void Stop()
    {
        _erwekaTimer?.Dispose();
        _tabmachineTimer?.Dispose();
        _fileTimer?.Dispose();
        _log.Info("Engine", "WatchDog 감시 중지");
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        _erwekaStatus.DisplayName    = config.Erweka.DisplayName;
        _tabmachineStatus.DisplayName = config.TabmachineIF.DisplayName;

        // 감시 주기 변경을 실행 중인 타이머에도 즉시 반영
        // (변경 전에는 _config만 갱신되고 타이머는 Start() 시점 주기로 계속 동작하던 버그)
        var erwekaInterval = TimeSpan.FromSeconds(config.Erweka.ProcessCheckSeconds);
        var tabInterval    = TimeSpan.FromSeconds(config.TabmachineIF.ProcessCheckSeconds);
        var fileInterval   = TimeSpan.FromMinutes(config.PdfFolder.FileActivityCheckMinutes);

        _erwekaTimer?.Change(erwekaInterval, erwekaInterval);
        _tabmachineTimer?.Change(tabInterval, tabInterval);
        _fileTimer?.Change(fileInterval, fileInterval);
        Interlocked.Increment(ref _fileConfigVersion);
        QueueFileActivityCheck();
    }

    public (ProgramStatus erweka, ProgramStatus tabmachine, FileActivityStatus file) GetCurrentStatus()
        => (_erwekaStatus, _tabmachineStatus, _fileStatus);

    public bool CheckErwekaRunningNow()
        => !string.IsNullOrWhiteSpace(_config.Erweka.ProcessName)
           && _processMonitor.IsRunning(_config.Erweka.ProcessName,
                                        _config.Erweka.ExecutablePath,
                                        _config.Erweka.Arguments)
           && (_config.Erweka.Port <= 0 || _processMonitor.IsPortListening(_config.Erweka.Port));

    // ─── 프로세스 감시 ────────────────────────────────────────────────────────

    // internal: 두 프로그램 일괄 점검 (테스트에서 직접 호출 가능)
    internal void CheckProcesses()
    {
        CheckErweka();
        CheckTabmachine();
    }

    // internal: 타이머 콜백 (각 프로그램별 독립 주기)
    internal void CheckErweka()
        => CheckProgram("Erweka", _config.Erweka, ref _erwekaStatus);

    internal void CheckTabmachine()
        => CheckProgram("TabmachineIF", _config.TabmachineIF, ref _tabmachineStatus);

    private void CheckProgram(string key, ProgramConfig cfg, ref ProgramStatus status)
    {
        // 프로세스 이름 미설정 — 이 사이트에서는 감시 대상이 아님 (예: ERWEKA 미사용 현장)
        if (string.IsNullOrWhiteSpace(cfg.ProcessName))
        {
            if (status.Status != HealthStatus.Disabled)
            {
                status.IsRunning = false;
                _trackers[key].ResetFailures();
                UpdateStatus(ref status, HealthStatus.Disabled, "프로세스 이름 미설정 — 감시 비활성화");
            }
            return;
        }

        var tracker   = _trackers[key];
        var isRunning = _processMonitor.IsRunning(cfg.ProcessName, cfg.ExecutablePath, cfg.Arguments);

        // 포트 감시 설정 시: 프로세스는 살아있어도 TCP LISTEN 상태가 아니면 장애로 간주
        // (TCP 서버 프로그램이 행(hang)되어 더 이상 요청을 받지 못하는 상태 검출용)
        var portOk = cfg.Port <= 0 || _processMonitor.IsPortListening(cfg.Port);

        // 이 인스턴스가 이 프로그램의 재시작을 전담하는지 여부
        // ERWEKA, TabmachineIF 모두 재시작 후 사용자가 화면에서 설정/시작 버튼을
        // 눌러야 하므로, 항상 대화형 세션(트레이 앱)만 재시작을 전담한다.
        // (서비스가 재시작하면 세션 0 격리로 인해 새 창이 사용자 화면에 보이지 않음)
        var shouldRestart = _isInteractiveSession;

        // 정상: 프로세스 실행 중 + (포트 감시 미설정 또는 포트 LISTEN 정상) — Failed 상태에서도 복구
        if (isRunning && portOk)
        {
            status.IsRunning      = true;
            status.LastSeenAlive  = DateTime.Now;

            // 재시작 비전담 인스턴스(트레이 등)는 복구 로그를 남기지 않음 — 전담 인스턴스 쪽에서 이미 기록함
            if (status.Status != HealthStatus.Healthy && shouldRestart)
                _log.Info(cfg.DisplayName, "프로세스 정상 감지 (복구됨)");

            var portInfo = cfg.Port > 0 ? $", 포트 {cfg.Port} LISTEN" : "";
            UpdateStatus(ref status, HealthStatus.Healthy,
                $"정상 실행 중 (PID: {_processMonitor.GetProcessInfo(cfg.ProcessName, cfg.ExecutablePath, cfg.Arguments)?.Pid}{portInfo})");
            tracker.ResetFailures();
            return;
        }

        status.IsRunning = isRunning;

        // 장애 원인 — 프로세스 자체가 없는지, 프로세스는 있으나 포트가 응답하지 않는지 구분
        var downReason = isRunning
            ? $"포트 {cfg.Port} 응답 없음 (TCP LISTEN 아님)"
            : "프로세스 미감지";

        // 재시작 비전담 인스턴스(서비스 등) — 상태 표시만 갱신하고 재시작/로그/트래커는 건드리지 않음
        if (!shouldRestart)
        {
            UpdateStatus(ref status, HealthStatus.Warning, $"{downReason} — 재시작은 트레이 앱(사용자 화면)이 처리합니다");
            return;
        }

        // 쿨다운 중이면 재시작 시도 생략
        if (tracker.IsInCooldown(cfg.RestartCooldownSeconds)) return;

        // 이미 MaxRestartAttempts 횟수를 소진한 경우 — 더 이상 재시작 시도 안 함
        if (tracker.ConsecutiveFailures >= cfg.MaxRestartAttempts)
        {
            UpdateStatus(ref status, HealthStatus.Failed,
                $"재시작 {cfg.MaxRestartAttempts}회 실패 — 수동 확인 필요");
            tracker.SetCooldown(cfg.RestartCooldownSeconds);
            return;
        }

        tracker.IncrementFailure();

        var isFailed = tracker.ConsecutiveFailures >= cfg.MaxRestartAttempts;  // 마지막 시도 여부
        if (isFailed)
        {
            _log.Error(cfg.DisplayName,
                $"재시작 {cfg.MaxRestartAttempts}회 연속 실패 — 수동 확인 필요. {cfg.RestartCooldownSeconds}초 후 재시도합니다.");
            UpdateStatus(ref status, HealthStatus.Failed,
                $"재시작 {tracker.ConsecutiveFailures}회 실패 — 수동 확인 필요 (재시도 대기 중)");
        }
        else
        {
            _log.Warn(cfg.DisplayName,
                $"{downReason} — 재시작 시도 ({tracker.ConsecutiveFailures}/{cfg.MaxRestartAttempts})");
            UpdateStatus(ref status, HealthStatus.Restarting,
                $"재시작 시도 중 ({tracker.ConsecutiveFailures}/{cfg.MaxRestartAttempts})");
        }

        var result = _restarter.TryRestart(cfg);
        tracker.SetCooldown(cfg.RestartCooldownSeconds);  // 설정값 적용 (기존 하드코딩 버그 수정)
        status.RestartCount++;
        status.LastRestartTime = DateTime.Now;

        if (result.Success)
        {
            _log.Info(cfg.DisplayName, $"재시작 성공 (PID: {result.Pid})");
            UpdateStatus(ref status, HealthStatus.Healthy, $"재시작 완료 (PID: {result.Pid})");
            tracker.ResetFailures();
        }
        else
        {
            _log.Error(cfg.DisplayName, $"재시작 실패: {result.Message}");
            UpdateStatus(ref status, isFailed ? HealthStatus.Failed : HealthStatus.Warning,
                $"재시작 실패: {result.Message}");
        }
    }

    private void UpdateStatus(ref ProgramStatus status, HealthStatus health, string message)
    {
        status.Status        = health;
        status.StatusMessage = message;
        ProgramStatusChanged?.Invoke(status);
    }

    // ─── 파일 활동 감시 ───────────────────────────────────────────────────────

    // internal: 타이머 콜백 + 테스트에서 직접 호출 가능
    internal void CheckFileActivity()
    {
        try
        {
            lock (_fileCheckLock)
            {
                var configVersion = Volatile.Read(ref _fileConfigVersion);
                var result = _fileMonitor.Check(_config.PdfFolder);

                if (configVersion != Volatile.Read(ref _fileConfigVersion))
                    return;

                _fileStatus = result;

                if (result.IsBacklogWarning)
                    TryLogFileMonitorWarning(result.StatusMessage);
                else if (result.IsIdleWarning)
                    TryLogFileMonitorWarning(result.StatusMessage);

                TryRaiseFileStatusChanged(result);
            }
        }
        catch (Exception ex)
        {
            TryLogFileMonitorError($"파일 활동 확인 중 예외 발생: {ex.Message}");
        }
    }

    private void QueueFileActivityCheck()
    {
        _ = Task.Run(CheckFileActivity);
    }

    private void TryRaiseFileStatusChanged(FileActivityStatus result)
    {
        try { FileStatusChanged?.Invoke(result); }
        catch (Exception ex) { TryLogFileMonitorError($"파일 상태 변경 이벤트 처리 중 예외 발생: {ex.Message}"); }
    }

    private void TryLogFileMonitorWarning(string message)
    {
        try { _log.Warn("FileMonitor", message); }
        catch (Exception ex) { TryLogFileMonitorError($"파일 활동 경고 로그 기록 중 예외 발생: {ex.Message}"); }
    }

    private void TryLogFileMonitorError(string message)
    {
        try { _log.Error("FileMonitor", message); }
        catch { /* 로그/이벤트 실패는 백그라운드 작업으로 전파하지 않음 */ }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
