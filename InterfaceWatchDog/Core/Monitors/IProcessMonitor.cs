using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Monitors;

public interface IProcessMonitor
{
    bool IsRunning(string processName);
    ProcessInfo? GetProcessInfo(string processName);
}
