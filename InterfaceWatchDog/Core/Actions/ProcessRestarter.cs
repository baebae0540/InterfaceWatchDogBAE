using System.Diagnostics;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Actions;

public class ProcessRestarter
{
    public RestartResult TryRestart(ProgramConfig config)
    {
        if (!config.CanRestart)
        {
            return RestartResult.Fail(
                $"실행 파일 경로가 설정되지 않았거나 파일이 없습니다. (경로: {config.ExecutablePath})");
        }

        try
        {
            KillExisting(config.ProcessName);

            var startInfo = new ProcessStartInfo
            {
                FileName = config.ExecutablePath,
                Arguments = config.Arguments,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(config.ExecutablePath) ?? ""
            };

            var process = Process.Start(startInfo);
            if (process == null)
                return RestartResult.Fail("프로세스 시작 실패 (Process.Start 반환값이 null)");

            // 시작 후 3초 대기 뒤 생존 확인
            Thread.Sleep(3000);

            return process.HasExited
                ? RestartResult.Fail("프로세스가 시작 후 즉시 종료됨")
                : RestartResult.Ok(process.Id);
        }
        catch (Exception ex)
        {
            return RestartResult.Fail($"재시작 중 예외 발생: {ex.Message}");
        }
    }

    private static void KillExisting(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        foreach (var p in Process.GetProcessesByName(processName))
        {
            try { p.Kill(entireProcessTree: true); }
            catch { /* 이미 종료된 프로세스 무시 */ }
        }
    }
}

public class RestartResult
{
    public bool Success { get; private set; }
    public int? Pid { get; private set; }
    public string Message { get; private set; } = "";

    public static RestartResult Ok(int pid) => new() { Success = true, Pid = pid, Message = "재시작 성공" };
    public static RestartResult Fail(string reason) => new() { Success = false, Message = reason };
}
