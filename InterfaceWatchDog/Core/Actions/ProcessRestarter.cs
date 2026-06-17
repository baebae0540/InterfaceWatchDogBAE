using System.Diagnostics;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.Core.Actions;

public class ProcessRestarter : IProcessRestarter
{
    public RestartResult TryRestart(TabmachineConfig config)
    {
        if (!config.CanRestart)
        {
            return RestartResult.Fail(
                $"실행 파일 경로가 설정되지 않았거나 파일이 없습니다. (경로: {config.ExecutablePath})");
        }

        try
        {
            KillExisting(config.ProcessName, config.ExecutablePath, config.Arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = config.ExecutablePath,
                Arguments = config.Arguments,
                // UseShellExecute = false: CreateProcess 직접 호출
                // → 관리자/서비스 컨텍스트에서 740(ERROR_ELEVATION_REQUIRED) 방지
                UseShellExecute = false,
                CreateNoWindow = false,
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

    // 실행 파일 경로 + 명령행 인수가 일치하는 프로세스만 종료한다.
    // (예: javaw.exe — 운영서버의 다른 Java 프로그램까지 함께 종료되는 것을 방지)
    private static void KillExisting(string processName, string executablePath, string commandLineContains)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        foreach (var p in ProcessMatcher.Find(processName, executablePath, commandLineContains))
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
