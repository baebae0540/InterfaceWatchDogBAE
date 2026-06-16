using System.Diagnostics;
using System.Management;

namespace InterfaceWatchDog.Core;

// 프로세스 이름 + 실행 파일 경로 + 명령행 인수로 감시 대상 프로세스를 식별한다.
// (예: javaw.exe — 운영서버의 여러 Java 프로그램이 동일한 javaw.exe(JDK 공용)를
//  공유하는 경우, "-jar ...Export Manager...exe" 같은 실행 인수로 ERWEKA만 구분한다)
internal static class ProcessMatcher
{
    public static IEnumerable<Process> Find(string processName, string executablePath = "", string commandLineContains = "")
    {
        if (string.IsNullOrWhiteSpace(processName)) return [];

        IEnumerable<Process> candidates = Process.GetProcessesByName(processName);

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var fullPath = GetFullPath(executablePath);
            candidates = candidates.Where(p =>
            {
                try { return string.Equals(GetFullPath(p.MainModule?.FileName), fullPath, StringComparison.OrdinalIgnoreCase); }
                catch { return false; } // 접근 권한 없는 프로세스 등은 대상에서 제외
            });
        }

        if (!string.IsNullOrWhiteSpace(commandLineContains))
        {
            var commandLines = GetCommandLines(processName);
            candidates = candidates.Where(p =>
                commandLines.TryGetValue(p.Id, out var cmd) &&
                cmd.Contains(commandLineContains, StringComparison.OrdinalIgnoreCase));
        }

        return candidates.ToArray();
    }

    // WMI로 각 프로세스의 전체 명령행(실행 파일 + 인수)을 조회한다.
    private static Dictionary<int, string> GetCommandLines(string processName)
    {
        var result = new Dictionary<int, string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = '{processName}.exe'");

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var pid = Convert.ToInt32(obj["ProcessId"]);
                result[pid] = obj["CommandLine"] as string ?? "";
            }
        }
        catch
        {
            // WMI 조회 실패 시 빈 목록 반환 — commandLineContains 필터는 매칭 없음으로 처리됨
        }

        return result;
    }

    private static string GetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        try { return Path.GetFullPath(path); }
        catch { return path; }
    }
}
