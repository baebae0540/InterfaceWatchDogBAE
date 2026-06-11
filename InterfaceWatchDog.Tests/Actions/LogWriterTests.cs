using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Tests.Actions;

public class LogWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LogWriter _log;

    public LogWriterTests()
    {
        Directory.CreateDirectory(_tempDir);
        _log = new LogWriter(_tempDir);
    }

    // ── Write → ReadTodayLogs 라운드트립 ──────────────────────────────────────

    [Fact]
    public void Info_ThenReadTodayLogs_ShouldRoundtrip()
    {
        _log.Info("TestSrc", "정보 메시지");
        var entries = _log.ReadTodayLogs();

        entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Info &&
            e.Source == "TestSrc" &&
            e.Message == "정보 메시지");
    }

    [Fact]
    public void Warn_ShouldWriteWarningLevel()
    {
        _log.Warn("WarnSrc", "경고 메시지");
        var entries = _log.ReadTodayLogs();

        entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Source == "WarnSrc");
    }

    [Fact]
    public void Error_ShouldWriteErrorLevel()
    {
        _log.Error("ErrSrc", "오류 메시지");
        var entries = _log.ReadTodayLogs();

        entries.Should().Contain(e => e.Level == LogLevel.Error && e.Source == "ErrSrc");
    }

    // ── 날짜별 로그 목록 정렬 ────────────────────────────────────────────────

    [Fact]
    public void GetAvailableLogDates_ShouldReturnDescendingOrder()
    {
        File.WriteAllText(Path.Combine(_tempDir, "watchdog_2026-01-01.log"), "");
        File.WriteAllText(Path.Combine(_tempDir, "watchdog_2026-06-09.log"), "");
        File.WriteAllText(Path.Combine(_tempDir, "watchdog_2025-12-31.log"), "");

        var dates = _log.GetAvailableLogDates();

        dates[0].Should().Be("2026-06-09");
        dates[1].Should().Be("2026-01-01");
        dates[2].Should().Be("2025-12-31");
    }

    [Fact]
    public void ReadLogsByDate_WithNonExistentDate_ShouldReturnEmpty()
    {
        var entries = _log.ReadLogsByDate("2000-01-01");
        entries.Should().BeEmpty();
    }

    // ── LogGenerated 이벤트 ───────────────────────────────────────────────────

    [Fact]
    public void LogGenerated_ShouldFireOnWrite()
    {
        LogEntry? received = null;
        _log.LogGenerated += e => received = e;

        _log.Info("Src", "이벤트 테스트");

        received.Should().NotBeNull();
        received!.Message.Should().Be("이벤트 테스트");
    }

    // ── 동시 쓰기 안전성 ──────────────────────────────────────────────────────

    [Fact]
    public void ConcurrentWrites_ShouldNotThrow()
    {
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => _log.Info("Concurrent", $"msg-{i}")))
            .ToArray();

        var act = () => Task.WaitAll(tasks);
        act.Should().NotThrow();

        var entries = _log.ReadTodayLogs();
        entries.Count(e => e.Source == "Concurrent").Should().Be(20);
    }

    // ── 존재하지 않는 디렉터리 → 빈 목록 ────────────────────────────────────

    [Fact]
    public void GetAvailableLogDates_WhenDirectoryNotExists_ShouldReturnEmpty()
    {
        var noDir = new LogWriter(Path.Combine(_tempDir, "nonexistent"));
        noDir.GetAvailableLogDates().Should().BeEmpty();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
