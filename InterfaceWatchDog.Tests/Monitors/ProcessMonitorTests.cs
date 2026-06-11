using InterfaceWatchDog.Core.Monitors;

namespace InterfaceWatchDog.Tests.Monitors;

public class ProcessMonitorTests
{
    private readonly ProcessMonitor _monitor = new();

    // ── 실행 중인 프로세스 감지 ───────────────────────────────────────────────

    [Fact]
    public void IsRunning_WithCurrentTestProcess_ShouldReturnTrue()
    {
        // dotnet test 호스트 프로세스 이름으로 확인
        // CI 환경에서는 "testhost" 또는 "dotnet"으로 실행됨
        var currentName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        _monitor.IsRunning(currentName).Should().BeTrue();
    }

    [Fact]
    public void IsRunning_WithNonExistentProcess_ShouldReturnFalse()
    {
        _monitor.IsRunning("zzz_nonexistent_proc_xyz_12345").Should().BeFalse();
    }

    // ── 입력 유효성 ───────────────────────────────────────────────────────────

    [Fact]
    public void IsRunning_WithNullProcessName_ShouldReturnFalse()
    {
        _monitor.IsRunning(null!).Should().BeFalse();
    }

    [Fact]
    public void IsRunning_WithWhitespaceProcessName_ShouldReturnFalse()
    {
        _monitor.IsRunning("   ").Should().BeFalse();
    }

    // ── GetProcessInfo ────────────────────────────────────────────────────────

    [Fact]
    public void GetProcessInfo_WithCurrentProcess_ShouldReturnValidInfo()
    {
        var currentName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        var info = _monitor.GetProcessInfo(currentName);

        info.Should().NotBeNull();
        info!.Pid.Should().BeGreaterThan(0);
        info.ProcessName.Should().NotBeNullOrEmpty();
    }
}
