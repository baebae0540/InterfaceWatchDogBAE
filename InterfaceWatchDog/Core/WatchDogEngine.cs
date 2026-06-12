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

    // 프로덕션 생성자 — 구체 클래스를 직접 생성
    public WatchDogEngine(AppConfig config, LogWriter log)
        : this(config, log, new ProcessMonitor(), new FileActivityMonitor(), new ProcessRestarter()) { }

    // 테스트/DI 생성자 — 인터페이스 주입 (InternalsVisibleTo로 테스트 프로젝트에서 접근)
    internal WatchDogEngine(AppConfig config, LogWriter log,
        IProcessMonitor processMonitor, IFileActivityMonitor fileMonitor, IProcessRestarter restarter)
    {
        _config         = config;
        _log            = log;
        _processMonitor = processMonitor;
        _fileMonitor    = fileMonitor;
        _restarter      = restarter;
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
    }

    public (ProgramStatus erweka, ProgramStatus tabmachine, FileActivityStatus file) GetCurrentStatus()
        => (_erwekaStatus, _tabmachineStatus, _fileStatus);

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
        var isRunning = _processMonitor.IsRunning(cfg.ProcessName);

        // 프로세스 실행 중이면 항상 정상 처리 (Failed 상태에서도 복구)
        if (isRunning)
        {
            status.IsRunning      = true;
            status.LastSeenAlive  = DateTime.Now;

            if (status.Status != HealthStatus.Healthy)
                _log.Info(cfg.DisplayName, "프로세스 정상 감지 (복구됨)");

            UpdateStatus(ref status, HealthStatus.Healthy,
                $"정상 실행 중 (PID: {_processMonitor.GetProcessInfo(cfg.ProcessName)?.Pid})");
            tracker.ResetFailures();
            return;
        }

        // 프로세스 없음 — 쿨다운 중이면 재시작 시도 생략
        status.IsRunning = false;
        if (tracker.IsInCooldown(cfg.RestartCooldownSeconds)) return;

        tracker.IncrementFailure();

        // 최대 재시도 횟수를 넘기면 Failed로 표시하되, 재시작 시도 자체는 쿨다운마다 계속 반복한다
        // (이전에는 여기서 영구적으로 포기하고 매 체크마다 ERROR만 반복 기록했음)
        var isFailed = tracker.ConsecutiveFailures >= cfg.MaxRestartAttempts;
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
                $"프로세스 미감지 — 재시작 시도 ({tracker.ConsecutiveFailures}/{cfg.MaxRestartAttempts})");
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
        var result = _fileMonitor.Check(_config.PdfFolder);
        _fileStatus = result;

        if (result.IsBacklogWarning)
            _log.Warn("FileMonitor", result.StatusMessage);
        else if (result.IsIdleWarning)
            _log.Warn("FileMonitor", result.StatusMessage);

        FileStatusChanged?.Invoke(result);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
