InterfaceWatchDog v1.2.1
ERWEKA / TabmachineIF 인터페이스 감시 시스템

■ 주요 기능
  - ERWEKA Export Manager 프로세스 및 TCP 포트 감시
  - TabmachineIF 프로세스 감시 및 자동 재시작
  - PDF 폴더 활동 감시 (유휴/백로그 경고)
  - 장애 발생 시 SYS_ALARM DB 알람 기록

■ 설정 파일
  프로그램 설정:  %ProgramData%\InterfaceWatchDog\config.json
  DB 연결 설정:   %ProgramData%\InterfaceWatchDog\dbconfig.json

■ 로그 파일
  %ProgramData%\InterfaceWatchDog\Logs\watchdog_YYYY-MM-DD.log

■ Windows 서비스 수동 관리
  등록:    InterfaceWatchDog.exe --install
  해제:    InterfaceWatchDog.exe --uninstall
  시작:    sc start InterfaceWatchDog
  중지:    sc stop InterfaceWatchDog
  상태:    sc query InterfaceWatchDog
