InterfaceWatchDog v1.0.0
인터페이스 감시 시스템

■ 설정 파일
  %ProgramData%\InterfaceWatchDog\config.json

■ 로그 파일
  %ProgramData%\InterfaceWatchDog\Logs\watchdog_YYYY-MM-DD.log

■ Windows 서비스 수동 관리
  등록:    InterfaceWatchDog.exe --install
  해제:    InterfaceWatchDog.exe --uninstall
  시작:    sc start InterfaceWatchDog
  중지:    sc stop InterfaceWatchDog
  상태:    sc query InterfaceWatchDog
