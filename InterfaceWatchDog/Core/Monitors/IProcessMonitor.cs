using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Monitors;

public interface IProcessMonitor
{
    // executablePath 지정 시: 동일한 이름의 프로세스가 여러 개 떠 있어도
    // 실행 파일 경로가 일치하는 프로세스만 대상으로 한다.
    // commandLineContains 지정 시: 실행 파일 경로가 같아도(예: javaw.exe — JDK 공용)
    // 명령행에 해당 문자열(예: -jar "...Export Manager...exe")이 포함된
    // 프로세스만 대상으로 한다.
    bool IsRunning(string processName, string executablePath = "", string commandLineContains = "");
    ProcessInfo? GetProcessInfo(string processName, string executablePath = "", string commandLineContains = "");

    // 지정 포트가 현재 TCP LISTEN 상태인지 확인 (TCP 서버 프로그램의 응답 가능 여부 점검용)
    bool IsPortListening(int port);
}
