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

    // ── 실행 파일 경로 기준 구분 (동일 이름의 다른 프로세스와 구분) ──────────────

    [Fact]
    public void IsRunning_WithMatchingExecutablePath_ShouldReturnTrue()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();
        var currentPath = current.MainModule!.FileName;

        _monitor.IsRunning(current.ProcessName, currentPath).Should().BeTrue();
    }

    [Fact]
    public void IsRunning_WithMismatchedExecutablePath_ShouldReturnFalse()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();

        _monitor.IsRunning(current.ProcessName, "C:\\other\\unrelated.exe").Should().BeFalse();
    }

    [Fact]
    public void GetProcessInfo_WithMismatchedExecutablePath_ShouldReturnNull()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();

        _monitor.GetProcessInfo(current.ProcessName, "C:\\other\\unrelated.exe").Should().BeNull();
    }

    // ── 명령행 인수 기준 구분 (동일 실행 파일을 공유하는 프로세스와 구분) ────────

    [Fact]
    public void IsRunning_WithMatchingCommandLineSubstring_ShouldReturnTrue()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();
        var exeName = Path.GetFileName(current.MainModule!.FileName);

        // 자기 자신의 실행 파일 이름은 명령행에 항상 포함됨
        _monitor.IsRunning(current.ProcessName, "", exeName).Should().BeTrue();
    }

    [Fact]
    public void IsRunning_WithNonMatchingCommandLineSubstring_ShouldReturnFalse()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();

        _monitor.IsRunning(current.ProcessName, "", "zzz_no_such_argument_xyz_12345").Should().BeFalse();
    }
}
