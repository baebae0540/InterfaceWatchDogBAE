using System.Diagnostics;
using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core.Monitors;

namespace InterfaceWatchDog.Tests.Integration;

// 실제 ProcessMonitor / FileActivityMonitor / ProcessRestarter / LogWriter를 그대로 사용하는 통합 테스트.
// 재시작 대상 프로세스는 ping.exe를 임시 폴더에 고유한 이름으로 복사해 "-t"(무한 핑) 옵션으로 사용한다
// (Windows 11의 notepad.exe는 실행 즉시 종료되는 리다이렉트 스텁이고, cmd.exe는 이름 변경 시
//  리소스 문자열을 찾지 못해 즉시 종료됨 — 재시작 검증에 부적합. 실제 시스템 프로세스를 건드리지
//  않기 위해 고유 이름의 복사본을 사용). 실행 중 콘솔 창이 잠시 나타날 수 있다.
// 별도 실행: dotnet test --filter Category=Integration
[Trait("Category", "Integration")]
public class WatchDogEngineIntegrationTests : IDisposable
{
    private const string DummyProcessName = "wd_test_proc";
    private const string DummyArguments   = "-t 127.0.0.1";

    private readonly string    _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string    _dummyExePath;
    private readonly LogWriter _log;
    private readonly string    _selfProcessName = Process.GetCurrentProcess().ProcessName;

    public WatchDogEngineIntegrationTests()
    {
        Directory.CreateDirectory(_tempDir);
        _log = new LogWriter(_tempDir);

        _dummyExePath = Path.Combine(_tempDir, DummyProcessName + ".exe");
        File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "PING.EXE"), _dummyExePath);

        KillAllDummyProcesses();
    }

    // 기본 설정: 두 프로그램 모두 "현재 테스트 프로세스"를 감시 대상으로 지정 →
    // 실제 ProcessMonitor가 항상 실행 중으로 판단하여 재시작이 발생하지 않는다.
    private AppConfig CreateConfig(PdfFolderConfig? pdfFolder = null, IntervalConfig? intervals = null) => new()
    {
        Erweka = new ProgramConfig
        {
            DisplayName            = "통합ERWEKA",
            ProcessName            = _selfProcessName,
            ExecutablePath         = "",
            MaxRestartAttempts     = 3,
            RestartCooldownSeconds = 0
        },
        TabmachineIF = new ProgramConfig
        {
            DisplayName            = "통합Tab",
            ProcessName            = _selfProcessName,
            ExecutablePath         = "",
            MaxRestartAttempts     = 3,
            RestartCooldownSeconds = 0
        },
        PdfFolder = pdfFolder ?? new PdfFolderConfig(),
        Intervals = intervals ?? new IntervalConfig()
    };

    private WatchDogEngine CreateEngine(AppConfig config) =>
        new(config, _log, new ProcessMonitor(), new FileActivityMonitor(), new ProcessRestarter());

    private static void KillAllDummyProcesses()
    {
        foreach (var p in Process.GetProcessesByName(DummyProcessName))
        {
            try { p.Kill(entireProcessTree: true); p.WaitForExit(2000); }
            catch { /* 이미 종료됨 */ }
        }
    }

    // ── 1. 실제 프로세스 다운 → 실제 재시작 ──────────────────────────────────

    [Fact]
    public void CheckProcesses_WhenProcessNotRunning_RealRestarterLaunchesAndBecomesHealthy()
    {
        var config = CreateConfig();
        config.Erweka.ProcessName    = DummyProcessName;
        config.Erweka.ExecutablePath = _dummyExePath;
        config.Erweka.Arguments      = DummyArguments;

        var engine = CreateEngine(config);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka.Should().NotBeNull();
        erweka!.Status.Should().Be(HealthStatus.Healthy);
        erweka.RestartCount.Should().Be(1);
        Process.GetProcessesByName(DummyProcessName).Should().NotBeEmpty();
    }

    // ── 2. 이미 실행 중인 프로세스 → 재시작 시도 없음 ────────────────────────

    [Fact]
    public void CheckProcesses_WhenProcessAlreadyRunning_ShouldNotRestart()
    {
        using var dummy = Process.Start(new ProcessStartInfo(_dummyExePath, DummyArguments) { UseShellExecute = false });
        Thread.Sleep(500); // 프로세스 등록 대기

        var config = CreateConfig();
        config.Erweka.ProcessName    = DummyProcessName;
        config.Erweka.ExecutablePath = _dummyExePath;
        config.Erweka.Arguments      = DummyArguments;

        var engine = CreateEngine(config);
        ProgramStatus? erweka = null;
        engine.ProgramStatusChanged += s => { if (s.Key == "Erweka") erweka = s; };

        engine.CheckProcesses();

        erweka!.Status.Should().Be(HealthStatus.Healthy);
        erweka.IsRunning.Should().BeTrue();
        erweka.RestartCount.Should().Be(0);
    }

    // ── 3. 실제 PDF 폴더 누적 경고 → 이벤트 + 로그 파일 기록 ──────────────────

    [Fact]
    public void CheckFileActivity_WithRealFolder_DetectsBacklogAndWritesLogFile()
    {
        var pdfDir = Path.Combine(_tempDir, "pdfs");
        Directory.CreateDirectory(pdfDir);
        for (int i = 0; i < 5; i++)
            File.WriteAllText(Path.Combine(pdfDir, $"doc{i}.pdf"), "x");

        var config = CreateConfig(pdfFolder: new PdfFolderConfig
        {
            Path = pdfDir, MaxBacklogCount = 3, MaxIdleMinutes = 60
        });

        var engine = CreateEngine(config);
        FileActivityStatus? received = null;
        engine.FileStatusChanged += s => received = s;

        engine.CheckFileActivity();

        received.Should().NotBeNull();
        received!.IsBacklogWarning.Should().BeTrue();
        received.FileCount.Should().Be(5);

        var logs = _log.ReadTodayLogs();
        logs.Should().Contain(l => l.Level == LogLevel.Warning && l.Source == "FileMonitor");
    }

    // ── 4. 설정 재적용 → 다음 파일 감시에 즉시 반영 ──────────────────────────

    [Fact]
    public void ReloadConfig_WithNewPdfFolder_AffectsNextFileActivityCheck()
    {
        var oldDir = Path.Combine(_tempDir, "old");
        var newDir = Path.Combine(_tempDir, "new");
        Directory.CreateDirectory(oldDir);
        Directory.CreateDirectory(newDir);
        File.WriteAllText(Path.Combine(newDir, "a.pdf"), "x");

        var engine = CreateEngine(CreateConfig(pdfFolder: new PdfFolderConfig
        {
            Path = oldDir, MaxBacklogCount = 50, MaxIdleMinutes = 60
        }));

        engine.CheckFileActivity();
        engine.GetCurrentStatus().file.FileCount.Should().Be(0);

        engine.ReloadConfig(CreateConfig(pdfFolder: new PdfFolderConfig
        {
            Path = newDir, MaxBacklogCount = 50, MaxIdleMinutes = 60
        }));
        engine.CheckFileActivity();

        engine.GetCurrentStatus().file.FileCount.Should().Be(1);
    }

    // ── 5. Start/Stop 실제 타이머 동작 ───────────────────────────────────────

    [Fact]
    public void StartStop_WithRealTimers_PeriodicallyRaisesStatusEvents_AndStopsCleanly()
    {
        var engine = CreateEngine(CreateConfig(
            intervals: new IntervalConfig { ProcessCheckSeconds = 1, FileActivityCheckMinutes = 60 }));

        var eventCount = 0;
        engine.ProgramStatusChanged += _ => Interlocked.Increment(ref eventCount);

        engine.Start();
        Thread.Sleep(2500);
        engine.Stop();

        var countAfterStop = eventCount;
        countAfterStop.Should().BeGreaterThanOrEqualTo(2);

        Thread.Sleep(1500);
        eventCount.Should().Be(countAfterStop);

        engine.Dispose();
    }

    public void Dispose()
    {
        KillAllDummyProcesses();
        Directory.Delete(_tempDir, recursive: true);
    }
}
