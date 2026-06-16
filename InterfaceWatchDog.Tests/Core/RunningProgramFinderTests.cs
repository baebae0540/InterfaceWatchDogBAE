using System.Net;
using System.Net.Sockets;
using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.Tests.Core;

public class RunningProgramFinderTests
{
    [Fact]
    public void GetVisibleWindows_ShouldNotThrow()
    {
        var act = () => RunningProgramFinder.GetVisibleWindows();
        act.Should().NotThrow();
    }

    [Fact]
    public void GetLaunchInfo_WithCurrentProcess_ShouldReturnValidInfo()
    {
        var current = System.Diagnostics.Process.GetCurrentProcess();

        var info = RunningProgramFinder.GetLaunchInfo(current.Id);

        info.Should().NotBeNull();
        info!.ProcessName.Should().Be(current.ProcessName);
        info.ExecutablePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetLaunchInfo_WithNonExistentPid_ShouldReturnNull()
    {
        var info = RunningProgramFinder.GetLaunchInfo(int.MaxValue);

        info.Should().BeNull();
    }

    // ── TCP 포트 감지 ─────────────────────────────────────────────────────────

    [Fact]
    public void GetListeningPorts_WithListeningSocket_ShouldIncludePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var ports = RunningProgramFinder.GetListeningPorts(Environment.ProcessId);

        ports.Should().Contain(port);
    }

    [Fact]
    public void GetListeningPorts_WithNonExistentPid_ShouldReturnEmpty()
    {
        var ports = RunningProgramFinder.GetListeningPorts(int.MaxValue);

        ports.Should().BeEmpty();
    }
}
