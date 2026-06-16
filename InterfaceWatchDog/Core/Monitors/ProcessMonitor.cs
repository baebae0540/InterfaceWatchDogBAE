using System.Diagnostics;
using System.Net.NetworkInformation;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.Core.Monitors;

public class ProcessMonitor : IProcessMonitor
{
    public bool IsRunning(string processName, string executablePath = "", string commandLineContains = "")
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;

        try
        {
            return ProcessMatcher.Find(processName, executablePath, commandLineContains).Any();
        }
        catch
        {
            return false;
        }
    }

    public bool IsPortListening(int port)
    {
        try
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return listeners.Any(ep => ep.Port == port);
        }
        catch
        {
            return false;
        }
    }

    public ProcessInfo? GetProcessInfo(string processName, string executablePath = "", string commandLineContains = "")
    {
        if (string.IsNullOrWhiteSpace(processName)) return null;

        try
        {
            var p = ProcessMatcher.Find(processName, executablePath, commandLineContains).FirstOrDefault();
            if (p == null) return null;

            return new ProcessInfo
            {
                ProcessName = p.ProcessName,
                Pid = p.Id,
                StartTime = p.StartTime,
                MemoryMB = p.WorkingSet64 / 1024 / 1024
            };
        }
        catch
        {
            return null;
        }
    }
}

public class ProcessInfo
{
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
    public DateTime StartTime { get; set; }
    public long MemoryMB { get; set; }
}
