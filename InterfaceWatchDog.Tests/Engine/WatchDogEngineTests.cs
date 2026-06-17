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
    private readonly IAlarmWriter        _aw  = Substitute.For<IAlarmWriter>();

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
            Erweka = new ErwekaConfig
            {
                DisplayName = "TestErweka",
                ProcessName = "test-erweka"
            },
            TabmachineIF = new TabmachineConfig
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

    private WatchDogEngine CreateEngine(bool isInteractiveSession = true, AlarmDbConfig? alarmDbConfig = null) =>
        new(_config, _log, _pm, _fm, _pr, _aw, alarmDbConfig, isInteractiveSession);

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

        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
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

    // ── 2. ERWEKA 다운 → 재시작 없이 알람 기록 ────────────────────────────

    [Fact]
    public void CheckProcesses_WhenErwekaDown_ShouldNotAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });

        var engine = CreateEngine();
        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-erweka"));
    }

    [Fact]
    public void CheckProcesses_WhenErwekaDown_StatusShouldBeFailed()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });

        var engine = CreateEngine();
        ProgramStatus? last = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") last = s; };

        engine.CheckProcesses();

        last.Should().NotBeNull();
        last!.Status.Should().Be(HealthStatus.Failed);
    }

    [Fact]
    public void CheckProcesses_WhenErwekaDownWithAlarmDb_ShouldWriteAlarm()
    {
        _config.DbConnectionVerified = true;
        var alarmDb = new AlarmDbConfig { Server = "test", Database = "testdb", PlantCode = "P1" };
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });
        _aw.WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
           .Returns(Task.FromResult(true));

        var engine = CreateEngine(alarmDbConfig: alarmDb);
        engine.CheckProcesses();

        // 비동기 Task.Run으로 실행되므로 약간의 대기
        Thread.Sleep(200);
        _aw.Received(1).WriteAlarmAsync(Arg.Any<string>(), "P1", Arg.Any<string>(), "test-erweka", Arg.Any<string>());
    }

    // ── 3. ERWEKA 반복 다운 → 알람 중복 방지 (1회만 Insert) ──────────────

    [Fact]
    public void CheckProcesses_WhenErwekaDownRepeatedly_ShouldWriteAlarmOnlyOnce()
    {
        _config.DbConnectionVerified = true;
        var alarmDb = new AlarmDbConfig { Server = "test", Database = "testdb", PlantCode = "P1" };
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 3003 });
        _aw.WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
           .Returns(Task.FromResult(true));

        var engine = CreateEngine(alarmDbConfig: alarmDb);

        engine.CheckProcesses();
        engine.CheckProcesses();
        engine.CheckProcesses();

        Thread.Sleep(200);
        _aw.Received(1).WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ── 4. ERWEKA 복구 후 재다운 → 새 알람 Insert ──────────────────────

    [Fact]
    public void CheckProcesses_WhenErwekaRecoveredAndDownAgain_ShouldWriteNewAlarm()
    {
        _config.DbConnectionVerified = true;
        var alarmDb = new AlarmDbConfig { Server = "test", Database = "testdb", PlantCode = "P1" };
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 4004 });
        _aw.WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
           .Returns(Task.FromResult(true));

        // 처음: 다운
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        var engine = CreateEngine(alarmDbConfig: alarmDb);
        engine.CheckProcesses();
        Thread.Sleep(200);

        // 복구
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 5555 });
        engine.CheckProcesses();

        // 다시 다운
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        engine.CheckProcesses();
        Thread.Sleep(200);

        _aw.Received(2).WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void CheckProcesses_WhenErwekaRecoveredFromFailed_StatusShouldBeHealthy()
    {
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 4004 });

        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        var engine = CreateEngine();
        engine.CheckProcesses();

        // 이후 프로세스 복구
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 5555 });

        ProgramStatus? recovered = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") recovered = s; };

        engine.CheckProcesses();

        recovered.Should().NotBeNull();
        recovered!.Status.Should().Be(HealthStatus.Healthy);
    }

    // ── ERWEKA: AlarmDb 미설정 시 알람 미전송 ────────────────────────────

    [Fact]
    public void CheckProcesses_WhenErwekaDownWithoutAlarmDb_ShouldNotWriteAlarm()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 2002 });

        var engine = CreateEngine();
        engine.CheckProcesses();

        Thread.Sleep(200);
        _aw.DidNotReceive().WriteAlarmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
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

    [Fact]
    public void CheckFileActivity_WhenSubscriberThrows_ShouldLogAndNotThrow()
    {
        _fm.Check(Arg.Any<PdfFolderConfig>()).Returns(new FileActivityStatus { StatusMessage = "정상" });

        var engine = CreateEngine();
        engine.FileStatusChanged += _ => throw new InvalidOperationException("subscriber failed");

        var act = () => engine.CheckFileActivity();

        act.Should().NotThrow();
        _log.ReadTodayLogs().Should().Contain(l =>
            l.Level == LogLevel.Error &&
            l.Source == "FileMonitor" &&
            l.Message.Contains("subscriber failed"));
    }

    [Fact]
    public async Task ReloadConfig_WhenFileCheckIsInFlight_ShouldIgnoreStaleFileStatus()
    {
        var firstCheckEntered = new ManualResetEventSlim();
        var releaseFirstCheck = new ManualResetEventSlim();
        var checkCount = 0;

        var staleStatus = new FileActivityStatus
        {
            IsFolderConfigured = true,
            FileCount = 10,
            StatusMessage = "old"
        };
        var freshStatus = new FileActivityStatus
        {
            IsFolderConfigured = true,
            FileCount = 1,
            StatusMessage = "new"
        };

        _fm.Check(Arg.Any<PdfFolderConfig>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref checkCount) == 1)
            {
                firstCheckEntered.Set();
                releaseFirstCheck.Wait(TimeSpan.FromSeconds(2));
                return staleStatus;
            }

            return freshStatus;
        });

        var engine = CreateEngine();
        var received = new List<FileActivityStatus>();
        var receivedLock = new object();
        engine.FileStatusChanged += s =>
        {
            lock (receivedLock)
            {
                received.Add(s);
            }
        };

        var firstCheck = Task.Run(engine.CheckFileActivity);

        firstCheckEntered.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
        engine.ReloadConfig(new AppConfig
        {
            Erweka = new ErwekaConfig { DisplayName = "새 ERWEKA" },
            TabmachineIF = new TabmachineConfig { DisplayName = "새 Tab" },
            PdfFolder = new PdfFolderConfig { Path = "new", Visible = true }
        });
        releaseFirstCheck.Set();

        await firstCheck.WaitAsync(TimeSpan.FromSeconds(2));
        SpinWait.SpinUntil(() =>
        {
            lock (receivedLock)
            {
                return received.Any(s => s.FileCount == 1);
            }
        },
            TimeSpan.FromSeconds(2)).Should().BeTrue();
        engine.GetCurrentStatus().file.FileCount.Should().Be(1);

        List<FileActivityStatus> snapshot;
        lock (receivedLock)
        {
            snapshot = received.ToList();
        }

        snapshot.Should().NotContain(s => s.FileCount == 10);
        snapshot.Should().ContainSingle(s => s.FileCount == 1);
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
        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
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
            Erweka       = new ErwekaConfig { DisplayName = "새 ERWEKA" },
            TabmachineIF = new TabmachineConfig { DisplayName = "새 Tab" }
        };
        engine.ReloadConfig(newConfig);

        var (erweka, tab, _) = engine.GetCurrentStatus();
        erweka.DisplayName.Should().Be("새 ERWEKA");
        tab.DisplayName.Should().Be("새 Tab");
    }

    // ── 9. 서비스(세션 0) 인스턴스 — ERWEKA는 세션 무관 재시작 안함, Tab은 트레이만 재시작 ──

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndErwekaDown_ShouldNotAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7007 });

        var engine = CreateEngine(isInteractiveSession: false);
        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndErwekaDown_StatusShouldBeFailed()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7008 });

        var engine = CreateEngine(isInteractiveSession: false);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka.Should().NotBeNull();
        erweka!.Status.Should().Be(HealthStatus.Failed);
        erweka.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndErwekaDown_ShouldNotAttemptRestartOnRepeatedChecks()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 7009 });

        var engine = CreateEngine(isInteractiveSession: false);

        for (int i = 0; i < 5; i++) engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
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
        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenErwekaRunningButPortNotListening_ShouldNotRestart()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);

        var engine = CreateEngine();
        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-erweka"));
    }

    [Fact]
    public void CheckProcesses_WhenErwekaRunningButPortNotListening_StatusShouldBeFailed()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);

        var engine = CreateEngine();
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka!.Status.Should().Be(HealthStatus.Failed);
    }

    [Fact]
    public void CheckProcesses_WhenErwekaRunningButPortNotListening_LogMentionsPort()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);

        var engine = CreateEngine();
        engine.CheckProcesses();

        var logs = _log.ReadTodayLogs();
        logs.Should().Contain(l => l.Source == "TestErweka" && l.Message.Contains("포트 9100"));
    }

    [Fact]
    public void CheckProcesses_WhenNotInteractiveSessionAndErwekaPortNotListening_StatusShouldBeFailedWithoutRestart()
    {
        _config.Erweka.Port = 9100;
        BothProcessesRunning();
        _pm.IsPortListening(9100).Returns(false);

        var engine = CreateEngine(isInteractiveSession: false);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka!.Status.Should().Be(HealthStatus.Failed);
        erweka.IsRunning.Should().BeTrue();
        _pr.DidNotReceive().TryRestart(Arg.Any<TabmachineConfig>());
    }

    [Fact]
    public void CheckProcesses_WhenErwekaPortConfiguredButProcessNotRunning_StatusShouldBeFailed()
    {
        _config.Erweka.Port = 9100;
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(false);
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-tab", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 8008 });

        var engine = CreateEngine();
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        _pr.DidNotReceive().TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-erweka"));
        erweka!.Status.Should().Be(HealthStatus.Failed);
        erweka.IsRunning.Should().BeFalse();
    }

    // ── 11. TabmachineIF도 동일한 정책(대화형 세션만 재시작 전담)을 따름 (세션 0 격리 회피) ──

    [Fact]
    public void CheckProcesses_WhenTabDownAndIsInteractive_ShouldAttemptRestart()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>()).Returns(true);
        _pm.GetProcessInfo("test-erweka", Arg.Any<string>()).Returns(new ProcessInfo { Pid = 1001 });
        _pm.IsRunning("test-tab", Arg.Any<string>()).Returns(false);
        _pr.TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-tab"))
           .Returns(RestartResult.Ok(2002));

        var engine = CreateEngine(isInteractiveSession: true);
        engine.CheckProcesses();

        _pr.Received(1).TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-tab"));
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

        _pr.DidNotReceive().TryRestart(Arg.Is<TabmachineConfig>(c => c.ProcessName == "test-tab"));
        tab!.Status.Should().Be(HealthStatus.Warning);
        tab.StatusMessage.Should().Contain("트레이 앱");
    }

    // ── 12. CheckErwekaRunningNow — 설정 열기 시 실시간 Tab3 활성화 판단 ────────

    [Fact]
    public void CheckErwekaRunningNow_WhenProcessNameEmpty_ReturnsFalse()
    {
        _config.Erweka.ProcessName = "";
        var engine = CreateEngine();

        engine.CheckErwekaRunningNow().Should().BeFalse();
        _pm.DidNotReceive().IsRunning(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void CheckErwekaRunningNow_WhenProcessRunningNoPort_ReturnsTrue()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var engine = CreateEngine();

        engine.CheckErwekaRunningNow().Should().BeTrue();
    }

    [Fact]
    public void CheckErwekaRunningNow_WhenProcessNotRunning_ReturnsFalse()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var engine = CreateEngine();

        engine.CheckErwekaRunningNow().Should().BeFalse();
    }

    [Fact]
    public void CheckErwekaRunningNow_WhenProcessRunningAndPortListening_ReturnsTrue()
    {
        _config.Erweka.Port = 9100;
        _pm.IsRunning("test-erweka", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _pm.IsPortListening(9100).Returns(true);
        var engine = CreateEngine();

        engine.CheckErwekaRunningNow().Should().BeTrue();
    }

    [Fact]
    public void CheckErwekaRunningNow_WhenProcessRunningButPortNotListening_ReturnsFalse()
    {
        _config.Erweka.Port = 9100;
        _pm.IsRunning("test-erweka", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _pm.IsPortListening(9100).Returns(false);
        var engine = CreateEngine();

        engine.CheckErwekaRunningNow().Should().BeFalse();
    }

    [Fact]
    public void CheckErwekaRunningNow_DoesNotMutateStatusOrFireEvents()
    {
        _pm.IsRunning("test-erweka", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var engine = CreateEngine();
        var events = new List<ProgramStatus>();
        engine.ProgramStatusChanged += s => events.Add(s);

        engine.CheckErwekaRunningNow();

        events.Should().BeEmpty();
        engine.GetCurrentStatus().erweka.Status.Should().Be(HealthStatus.Unknown);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
