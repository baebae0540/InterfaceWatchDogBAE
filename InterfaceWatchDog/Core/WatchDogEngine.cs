using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core.Monitors;

namespace InterfaceWatchDog.Core;

public class WatchDogEngine : IDisposable
{
    private AppConfig _config;
    private readonly LogWriter _log;
    private readonly ProcessMonitor _processMonitor = new();
    private readonly ProcessRestarter _restarter = new();
    private readonly FileActivityMonitor _fileMonitor = new();

    private System.Threading.Timer? _processTimer;
    private System.Threading.Timer? _fileTimer;

    private readonly Dictionary<string, RestartTracker> _trackers = new()
    {
        ["Erweka"] = new RestartTracker(),
        ["TabmachineIF"] = new RestartTracker()
    };

    private ProgramStatus _erwekaStatus = new() { Key = "Erweka", DisplayName = "ERWEKA Export Manager" };
    private ProgramStatus _tabmachineStatus = new() { Key = "TabmachineIF", DisplayName = "TabmachineIF" };
    private FileActivityStatus _fileStatus = new();

    public event Action<ProgramStatus>? ProgramStatusChanged;
    public event Action<FileActivityStatus>? FileStatusChanged;

    public WatchDogEngine(AppConfig config, LogWriter log)
    {
        _config = config;
        _log = log;
        _log.LogGenerated += _ => { }; // 엔진은 로그 이벤트 전달 역할만 함
    }

    public void Start()
    {
        _log.Info("Engine", "WatchDog 감시 시작");

        var processInterval = TimeSpan.FromSeconds(_config.Intervals.ProcessCheckSeconds);
        var fileInterval = TimeSpan.FromMinutes(_config.Intervals.FileActivityCheckMinutes);

        _processTimer = new System.Threading.Timer(_ => CheckProcesses(), null,
            TimeSpan.Zero, processInterval);

        _fileTimer = new System.Threading.Timer(_ => CheckFileActivity(), null,
            TimeSpan.FromSeconds(10), fileInterval);
    }

    public void Stop()
    {
        _processTimer?.Dispose();
        _fileTimer?.Dispose();
        _log.Info("Engine", "WatchDog 감시 중지");
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        _erwekaStatus.DisplayName = config.Erweka.DisplayName;
        _tabmachineStatus.DisplayName = config.TabmachineIF.DisplayName;
    }

    public (ProgramStatus erweka, ProgramStatus tabmachine, FileActivityStatus file) GetCurrentStatus()
        => (_erwekaStatus, _tabmachineStatus, _fileStatus);

    // ─── 프로세스 감시 ────────────────────────────────────────────────────────

    private void CheckProcesses()
    {
        CheckProgram("Erweka", _config.Erweka, ref _erwekaStatus);
        CheckProgram("TabmachineIF", _config.TabmachineIF, ref _tabmachineStatus);
    }

    private void CheckProgram(string key, ProgramConfig cfg, ref ProgramStatus status)
    {
        var tracker = _trackers[key];

        // 재시작 쿨다운 중이면 생략
        if (tracker.IsInCooldown(cfg.RestartCooldownSeconds)) return;

        var isRunning = _processMonitor.IsRunning(cfg.ProcessName);

        if (isRunning)
        {
            status.IsRunning = true;
            status.LastSeenAlive = DateTime.Now;

            if (status.Status != HealthStatus.Healthy)
            {
                _log.Info(cfg.DisplayName, "프로세스 정상 감지");
            }

            UpdateStatus(ref status, HealthStatus.Healthy,
                $"정상 실행 중 (PID: {_processMonitor.GetProcessInfo(cfg.ProcessName)?.Pid})");
            tracker.ResetFailures();
        }
        else
        {
            status.IsRunning = false;
            tracker.IncrementFailure();

            if (tracker.ConsecutiveFailures >= cfg.MaxRestartAttempts)
            {
                UpdateStatus(ref status, HealthStatus.Failed,
                    $"재시작 {cfg.MaxRestartAttempts}회 실패 — 수동 확인 필요");
                _log.Error(cfg.DisplayName,
                    $"재시작 {cfg.MaxRestartAttempts}회 연속 실패. 수동 개입 필요.");
                return;
            }

            _log.Warn(cfg.DisplayName,
                $"프로세스 미감지 — 재시작 시도 ({tracker.ConsecutiveFailures}/{cfg.MaxRestartAttempts})");
            UpdateStatus(ref status, HealthStatus.Restarting,
                $"재시작 시도 중 ({tracker.ConsecutiveFailures}/{cfg.MaxRestartAttempts})");

            var result = _restarter.TryRestart(cfg);
            tracker.SetCooldown();
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
                UpdateStatus(ref status, HealthStatus.Warning, $"재시작 실패: {result.Message}");
            }
        }
    }

    private void UpdateStatus(ref ProgramStatus status, HealthStatus health, string message)
    {
        status.Status = health;
        status.StatusMessage = message;
        ProgramStatusChanged?.Invoke(status);
    }

    // ─── 파일 활동 감시 ───────────────────────────────────────────────────────

    private void CheckFileActivity()
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

internal class RestartTracker
{
    public int ConsecutiveFailures { get; private set; }
    private DateTime _cooldownUntil = DateTime.MinValue;

    public bool IsInCooldown(int cooldownSeconds)
        => DateTime.Now < _cooldownUntil;

    public void IncrementFailure() => ConsecutiveFailures++;
    public void ResetFailures() => ConsecutiveFailures = 0;
    public void SetCooldown() => _cooldownUntil = DateTime.Now.AddSeconds(30);
}
