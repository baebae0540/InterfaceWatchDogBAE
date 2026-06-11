using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Tests.Models;

public class LogEntryTests
{
    // ── LevelText 매핑 ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(LogLevel.Info,    "INFO")]
    [InlineData(LogLevel.Warning, "WARN")]
    [InlineData(LogLevel.Error,   "ERROR")]
    public void LevelText_ShouldReturnCorrectString(LogLevel level, string expected)
    {
        var entry = new LogEntry { Level = level };
        entry.LevelText.Should().Be(expected);
    }

    // ── LevelColor ARGB 검증 ──────────────────────────────────────────────────

    [Theory]
    [InlineData(LogLevel.Info,    33,  150, 243)]
    [InlineData(LogLevel.Warning, 255, 152,   0)]
    [InlineData(LogLevel.Error,   244,  67,  54)]
    public void LevelColor_ShouldReturnCorrectRgb(LogLevel level, int r, int g, int b)
    {
        var entry = new LogEntry { Level = level };
        entry.LevelColor.R.Should().Be((byte)r);
        entry.LevelColor.G.Should().Be((byte)g);
        entry.LevelColor.B.Should().Be((byte)b);
    }

    // ── ToString 포맷 ─────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ShouldIncludeAllFields()
    {
        var ts    = new DateTime(2026, 6, 9, 10, 30, 0);
        var entry = new LogEntry
        {
            Timestamp = ts,
            Level     = LogLevel.Warning,
            Source    = "Engine",
            Message   = "테스트 메시지"
        };

        var result = entry.ToString();

        result.Should().Contain("2026-06-09 10:30:00");
        result.Should().Contain("WARN");
        result.Should().Contain("Engine");
        result.Should().Contain("테스트 메시지");
    }

    [Fact]
    public void ToString_ShouldMatchExpectedFormat()
    {
        var ts    = new DateTime(2026, 1, 5, 8, 5, 3);
        var entry = new LogEntry
        {
            Timestamp = ts,
            Level     = LogLevel.Info,
            Source    = "Src",
            Message   = "msg"
        };

        // [2026-01-05 08:05:03] [INFO ] [Src] msg
        entry.ToString().Should().StartWith("[2026-01-05 08:05:03]");
        entry.ToString().Should().Contain("[INFO ]");
        entry.ToString().Should().Contain("[Src]");
    }

    // ── 기본 타임스탬프 ───────────────────────────────────────────────────────

    [Fact]
    public void DefaultTimestamp_ShouldBeCloseToNow()
    {
        var before = DateTime.Now;
        var entry  = new LogEntry();
        var after  = DateTime.Now;

        entry.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
