using System.Diagnostics;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Monitors;

public class ProcessMonitor : IProcessMonitor
{
    public bool IsRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;

        try
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public ProcessInfo? GetProcessInfo(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return null;

        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return null;

            var p = processes[0];
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
