using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core.Monitors;
using NSubstitute;

namespace InterfaceWatchDog.Tests.Engine;

public class WatchDogEngineTests : IDisposable
{
    // ── 목업 ─────────────────────────────────────────────────────────────────
    private readonly IProcessMonitor     _pm  = Substitute.For<IProcessMonitor>();
    private readonly IFileActivityMonitor _fm  = Substitute.For<IFileActivityMonitor>();
    private readonly IProcessRestarter   _pr  = Substitute.For<IProcessRestarter>();

    // ── 실제 협력자 (임시 폴더 LogWriter) ────────────────────────────────────
    private readonly string    _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LogWriter _log;
    private readonly AppConfig _config;

    public WatchDogEngineTests()
    {
        Directory.CreateDirectory(_tempDir);
        _log = new LogWriter(_tempDir);

        _config = new AppConfig
        {
            Erweka = new ProgramConfig
            {
                DisplayName            = "TestErweka",
                ProcessName            = "test-erweka",
                ExecutablePath         = "C:\\fake\\erweka.exe",
                MaxRestartAttempts     = 3,
                RestartCooldownSeconds = 0   // 쿨다운 0 → 연속 호출 가능
            },
            TabmachineIF = new ProgramConfig
            {
                DisplayName            = "TestTab",
                ProcessName            = "test-tab",
                ExecutablePath         = "C:\\fake\\tab.exe",
                MaxRestartAttempts     = 3,
                RestartCooldownSeconds = 0
            }
        };

        // 파일 감시 기본 응답
        _fm.Check(Arg.Any<PdfFolderConfig>()).Returns(new FileActivityStatus());
    }

    private WatchDogEngine CreateEngine(bool isInteractiveSession = true) =>
        new(_config, _log, _pm, _fm, _pr, isInteractiveSession);

    private void BothProcessesRunning()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 1001 });
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 1002 });
    }

    // ── 1. 두 프로세스 모두 정상 → 재시작 시도 없음 ──────────────────────────

    [Fact]
    public void CheckProcesses_WhenBothRunning_ShouldNotAttemptRestart()
    {
        BothProcessesRunning();
        var engine = CreateEngine();

        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenBothRunning_StatusShouldBeHealthy()
    {
        BothProcessesRunning();
        var engine = CreateEngine();
        var statuses = new List<ProgramStatus>();
        engine.ProgramStatusChanged += s => statuses.Add(s);

        engine.CheckProcesses();

        statuses.Should().OnlyContain(s => s.Status == HealthStatus.Healthy);
    }

    // ── 2. 프로세스 다운 → 재시작 성공 ──────────────────────────────────────

    [Fact]
    public void CheckProcesses_WhenErwekaDown_ShouldAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });
        _pr.TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-erweka"))
           .Returns(RestartResult.Ok(9999));

        var engine = CreateEngine();
        engine.CheckProcesses();

        _pr.Received(1).TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-erweka"));
    }

    [Fact]
    public void CheckProcesses_WhenRestartSucceeds_StatusShouldBeHealthy()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });
        _pr.TryRestart(Arg.Any<ProgramConfig>()).Returns(RestartResult.Ok(9999));

        var engine = CreateEngine();
        ProgramStatus? last = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") last = s; };

        engine.CheckProcesses();

        last.Should().NotBeNull();
        last!.Status.Should().Be(HealthStatus.Healthy);
        last.RestartCount.Should().Be(1);
    }

    // ── 3. 최대 재시도 초과 → Failed ─────────────────────────────────────────

    [Fact]
    public void CheckProcesses_WhenMaxRetriesExceeded_StatusShouldBeFailed()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 3003 });
        _pr.TryRestart(Arg.Any<ProgramConfig>()).Returns(RestartResult.Fail("파일 없음"));

        var engine = CreateEngine();
        var erwekaStatuses = new List<ProgramStatus>();
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erwekaStatuses.Add(s); };

        // MaxRestartAttempts=3 → 로직: failures 1,2→Restarting, 3→Failed
        // failures>=3 이후에도 재시작 시도 자체는 쿨다운마다 계속 반복된다
        engine.CheckProcesses(); // failures=1, restart#1 (실패)
        engine.CheckProcesses(); // failures=2, restart#2 (실패)
        engine.CheckProcesses(); // failures=3, Failed 상태 진입 + restart#3 (실패)

        erwekaStatuses.Last().Status.Should().Be(HealthStatus.Failed);
        _pr.Received(3).TryRestart(Arg.Any<ProgramConfig>());
    }

    // ── 4. Failed 상태에서 프로세스 복구 → Healthy 복귀 ─────────────────────

    [Fact]
    public void CheckProcesses_WhenProcessRecoveredFromFailed_StatusShouldBeHealthy()
    {
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 4004 });
        _pr.TryRestart(Arg.Any<ProgramConfig>()).Returns(RestartResult.Fail("파일 없음"));

        // 처음 4번: 다운 → Failed 진입
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        var engine = CreateEngine();
        for (int i = 0; i < 4; i++) engine.CheckProcesses();

        // 이후 프로세스 복구
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 5555 });

        ProgramStatus? recovered = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") recovered = s; };

        engine.CheckProcesses();

        recovered.Should().NotBeNull();
        recovered!.Status.Should().Be(HealthStatus.Healthy);
    }

    // ── 5. 파일 활동 경고 이벤트 전파 ────────────────────────────────────────

    [Fact]
    public void CheckFileActivity_WhenBacklogWarning_ShouldRaiseFileStatusChangedEvent()
    {
        var warningStatus = new FileActivityStatus
        {
            IsFolderConfigured = true,
            IsBacklogWarning   = true,
            FileCount          = 100,
            StatusMessage      = "누적 경고"
        };
        _fm.Check(Arg.Any<PdfFolderConfig>()).Returns(warningStatus);

        var engine = CreateEngine();
        FileActivityStatus? received = null;
        engine.FileStatusChanged += s => received = s;

        engine.CheckFileActivity();

        received.Should().NotBeNull();
        received!.IsBacklogWarning.Should().BeTrue();
        received.FileCount.Should().Be(100);
    }

    [Fact]
    public void CheckFileActivity_WhenIdleWarning_ShouldRaiseEventWithIdleFlag()
    {
        var idleStatus = new FileActivityStatus
        {
            IsFolderConfigured = true,
            IsIdleWarning      = true,
            StatusMessage      = "유휴 경고"
        };
        _fm.Check(Arg.Any<PdfFolderConfig>()).Returns(idleStatus);

        var engine = CreateEngine();
        FileActivityStatus? received = null;
        engine.FileStatusChanged += s => received = s;

        engine.CheckFileActivity();

        received!.IsIdleWarning.Should().BeTrue();
    }

    // ── 6. 프로세스 이름 미설정 → 감시 비활성화 (Disabled) ───────────────────

    [Fact]
    public void CheckProcesses_WhenProcessNameBlank_StatusShouldBeDisabledAndNotChecked()
    {
        _config.Erweka.ProcessName = "";
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 6006 });

        var engine = CreateEngine();
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka.Should().NotBeNull();
        erweka!.Status.Should().Be(HealthStatus.Disabled);
        erweka.IsRunning.Should().BeFalse();
        _pm.DidNotReceive().IsRunning("");
        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenProcessNameBlank_ShouldNotRaiseRepeatedEvents()
    {
        _config.Erweka.ProcessName = "";
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 6007 });

        var engine = CreateEngine();
        var erwekaEvents = new List<ProgramStatus>();
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erwekaEvents.Add(s); };

        engine.CheckProcesses();
        engine.CheckProcesses();

        erwekaEvents.Should().HaveCount(1);
    }

    // ── 7. GetCurrentStatus 기본값 ────────────────────────────────────────────

    [Fact]
    public void GetCurrentStatus_BeforeAnyCheck_ShouldReturnUnknownStatus()
    {
        var engine = CreateEngine();
        var (erweka, tab, file) = engine.GetCurrentStatus();

        erweka.Status.Should().Be(HealthStatus.Unknown);
        tab.Status.Should().Be(HealthStatus.Unknown);
        file.IsFolderConfigured.Should().BeFalse();
    }

    // ── 8. ReloadConfig 반영 ─────────────────────────────────────────────────

    [Fact]
    public void ReloadConfig_ShouldUpdateDisplayName()
    {
        var engine = CreateEngine();

        var newConfig = new AppConfig
        {
            Erweka       = new ProgramConfig { DisplayName = "새 ERWEKA" },
            TabmachineIF = new ProgramConfig { DisplayName = "새 Tab" }
        };
        engine.ReloadConfig(newConfig);

        var (erweka, tab, _) = engine.GetCurrentStatus();
        erweka.DisplayName.Should().Be("새 ERWEKA");
        tab.DisplayName.Should().Be("새 Tab");
    }

    // ── 9. 서비스(세션 0) 인스턴스 — 재시작은 트레이 앱(대화형 세션)이 전담, 상태 표시만 갱신 ──

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndProcessDown_ShouldNotAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7007 });

        var engine = CreateEngine(isInteractiveSession: false);
        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndProcessDown_StatusShouldBeWarning()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7008 });

        var engine = CreateEngine(isInteractiveSession: false);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka.Should().NotBeNull();
        erweka!.Status.Should().Be(HealthStatus.Warning);
        erweka.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndProcessDown_TrackerShouldNotBeAffected()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7009 });

        var engine = CreateEngine(isInteractiveSession: false);

        // 반복 호출해도 재시작 시도/실패 카운트가 누적되지 않아야 함
        for (int i = 0; i < 5; i++) engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndProcessRunning_StatusShouldBeHealthy()
    {
        BothProcessesRunning();

        var engine = CreateEngine(isInteractiveSession: false);
        var statuses = new List<ProgramStatus>();
        engine.ProgramStatusChanged += s => statuses.Add(s);

        engine.CheckProcesses();

        statuses.Should().OnlyContain(s => s.Status == HealthStatus.Healthy);
    }

    // ── 10. TCP 포트 감시 (Port > 0 — 프로세스 정상이어도 포트 미응답 시 장애로 판단) ──

    [Fact]
    public void CheckProcesses_WhenProcessRunningAndPortListening_StatusShouldBeHealthy()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(true);

        var engine = CreateEngine();
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka!.Status.Should().Be(HealthStatus.Healthy);
        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenProcessRunningButPortNotListening_ShouldAttemptRestart()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);
        _pr.TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-erweka"))
           .Returns(RestartResult.Ok(9999));

        var engine = CreateEngine();
        engine.CheckProcesses();

        _pr.Received(1).TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-erweka"));
    }

    [Fact]
    public void CheckProcesses_WhenProcessRunningButPortNotListening_LogMentionsPort()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);
        _pr.TryRestart(Arg.Any<ProgramConfig>()).Returns(RestartResult.Fail("실행 파일 없음"));

        var engine = CreateEngine();
        engine.CheckProcesses();

        var logs = _log.ReadTodayLogs();
        logs.Should().Contain(l => l.Source == "TestErweka" && l.Message.Contains("포트 9100"));
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndPortNotListening_StatusShouldBeWarningWithoutRestart()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);

        var engine = CreateEngine(isInteractiveSession: false);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka!.Status.Should().Be(HealthStatus.Warning);
        erweka.IsRunning.Should().BeTrue();
        _pr.DidNotReceive().TryRestart(Arg.Any<ProgramConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenPortConfiguredButProcessNotRunning_ShouldUseProcessDownReason()
    {
        _config.Erweka.Port = 9100;
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 8008 });
        _pr.TryRestart(Arg.Any<ProgramConfig>()).Returns(RestartResult.Ok(9999));

        var engine = CreateEngine();
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        // 프로세스 자체가 없을 때는 포트 상태와 무관하게 재시작 시도
        _pr.Received(1).TryRestart(Arg.Any<ProgramConfig>());
        erweka!.IsRunning.Should().BeFalse();
    }

    // ── 11. TabmachineIF도 동일한 정책(대화형 세션만 재시작 전담)을 따름 (세션 0 격리 회피) ──

    [Fact]
    public void CheckProcesses_WhenTabDownAndIsInteractive_ShouldAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 1001 });
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(false);
        _pr.TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-tab"))
           .Returns(RestartResult.Ok(2002));

        var engine = CreateEngine(isInteractiveSession: true);
        engine.CheckProcesses();

        _pr.Received(1).TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-tab"));
    }

    [Fact]
    public void CheckProcesses_WhenTabDownAndNotInteractive_ShouldNotAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 1001 });
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(false);

        // 서비스(세션 0) 인스턴스 — 대화형 세션이 아니므로 TabmachineIF는 재시작하지 않음
        // (항상 트레이가 전담)
        var engine = CreateEngine(isInteractiveSession: false);
        ProgramStatus? tab = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "TabmachineIF") tab = s; };

        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Is<ProgramConfig>(c => c.ProcessName == "test-tab"));
        tab!.Status.Should().Be(HealthStatus.Warning);
        tab.StatusMessage.Should().Contain("트레이 앱");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
