using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Tests.Models;

public class ProgramStatusTests
{
    // ── StatusText 한국어 매핑 ────────────────────────────────────────────────

    [Theory]
    [InlineData(HealthStatus.Healthy,    "정상")]
    [InlineData(HealthStatus.Warning,    "경고")]
    [InlineData(HealthStatus.Restarting, "재시작 중")]
    [InlineData(HealthStatus.Failed,     "복구 실패")]
    [InlineData(HealthStatus.Unknown,    "확인 중")]
    [InlineData(HealthStatus.Disabled,   "감시 안함")]
    public void StatusText_ShouldReturnKoreanLabel(HealthStatus status, string expected)
    {
        var s = new ProgramStatus { Status = status };
        s.StatusText.Should().Be(expected);
    }

    // ── StatusColor RGB 검증 ──────────────────────────────────────────────────

    [Theory]
    [InlineData(HealthStatus.Healthy,     76, 175,  80)]
    [InlineData(HealthStatus.Warning,    255, 152,   0)]
    [InlineData(HealthStatus.Restarting,  33, 150, 243)]
    [InlineData(HealthStatus.Failed,     244,  67,  54)]
    [InlineData(HealthStatus.Unknown,    158, 158, 158)]
    [InlineData(HealthStatus.Disabled,   189, 193, 204)]
    public void StatusColor_ShouldReturnCorrectRgb(HealthStatus status, int r, int g, int b)
    {
        var s = new ProgramStatus { Status = status };
        s.StatusColor.R.Should().Be((byte)r);
        s.StatusColor.G.Should().Be((byte)g);
        s.StatusColor.B.Should().Be((byte)b);
    }

    // ── FileActivityStatus 경고 플래그 ───────────────────────────────────────

    [Fact]
    public void FileActivityStatus_WhenNeitherWarning_ShouldHaveBothFlagsOff()
    {
        var status = new FileActivityStatus
        {
            IsFolderConfigured = true,
            IsIdleWarning      = false,
            IsBacklogWarning   = false
        };

        status.IsIdleWarning.Should().BeFalse();
        status.IsBacklogWarning.Should().BeFalse();
    }

    [Fact]
    public void FileActivityStatus_WhenBothWarnings_ShouldHaveBothFlagsOn()
    {
        var status = new FileActivityStatus
        {
            IsIdleWarning    = true,
            IsBacklogWarning = true
        };

        status.IsIdleWarning.Should().BeTrue();
        status.IsBacklogWarning.Should().BeTrue();
    }
}
