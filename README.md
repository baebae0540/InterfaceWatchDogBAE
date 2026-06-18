# InterfaceWatchDog

ERWEKA Export Manager와 TabmachineIF 인터페이스 프로세스를 실시간으로 감시하고, 장애 발생 시 자동 재시작 및 알람을 제공하는 Windows 모니터링 프로그램입니다.

- **버전**: v1.2.1
- **플랫폼**: .NET 8.0 / Windows Forms / win-x64
- **배포**: 단일 실행 파일 (Self-Contained)

## 주요 기능

### 프로세스 감시
- **ERWEKA Export Manager**: 프로세스 실행 상태 + TCP 포트 응답 확인
- **TabmachineIF**: 프로세스 실행 상태 확인 및 자동 재시작
- 프로세스 이름, 실행 경로, 명령줄 인수를 조합한 정밀 매칭 (동일 이름 프로세스 구별)
- 설정 가능한 감시 주기 (10~300초)

### 자동 재시작
- TabmachineIF 프로세스 종료 감지 시 자동 재시작
- 최대 재시작 시도 횟수 제한 (기본 3회, 1~10회 설정 가능)
- 재시작 간 쿨다운 대기 (기본 60초)
- 연속 실패 시 Failed 상태 전환 및 SYS_ALARM DB 알람 기록

### PDF 폴더 감시
- ERWEKA 출력 PDF 폴더의 파일 활동 모니터링
- **유휴 경고**: 설정 시간 동안 신규 PDF 미생성 시 알림
- **백로그 경고**: 미처리 파일 수 임계치 초과 시 알림
- 감시 기능 활성화/비활성화 선택 가능

### 알림 및 알람
- **시스템 트레이 아이콘**: 상태별 색상 표시 (녹색=정상, 주황=경고, 파랑=재시작중, 빨강=장애, 회색=비활성)
- **트레이 풍선 알림**: 장애/경고 발생 시 즉시 알림
- **DB 알람 기록**: ERWEKA 장애 및 TabmachineIF 재시작 최종 실패 시 SQL Server `SYS_ALARM` 테이블에 비동기 알람 기록 (중복 방지, 복구 후 재발 시 새 알람)
- **로그 파일**: 일별 로그 파일 자동 생성

### 로그 뷰어
- 달력 기반 날짜 선택으로 과거 로그 조회
- 로그 레벨별 색상 구분 (INFO/WARN/ERROR)
- 실시간 로그 스트리밍

### Windows 서비스
- 사용자 로그인 없이 부팅 시 자동 감시 시작
- 설정 파일 변경 감지 및 핫리로드 (서비스 재시작 불필요)
- 트레이 앱에서 서비스 설치/제거 (UAC 지원)

## 실행 모드

| 모드 | 설명 | 재시작 수행 |
|------|------|:-----------:|
| **트레이 앱** (기본) | 시스템 트레이에서 대화형 실행, 상태 대시보드 제공 | O |
| **Windows 서비스** | 백그라운드 실행, 부팅 시 자동 시작 | X (Session 0 제약) |

트레이 앱과 Windows 서비스를 함께 사용하면, 서비스가 부팅 시 감시를 시작하고 트레이 앱이 재시작 및 UI를 담당합니다.

## 프로젝트 구조

```
InterfaceWatchDog/
├── InterfaceWatchDog/              # 메인 애플리케이션
│   ├── Core/
│   │   ├── Actions/                # LogWriter, ProcessRestarter, AlarmWriter
│   │   ├── Models/                 # AppConfig, ProgramStatus, LogEntry 등
│   │   └── Monitors/              # ProcessMonitor, FileActivityMonitor
│   │   ├── ConfigManager.cs        # 설정 파일 관리
│   │   ├── WatchDogEngine.cs       # 핵심 감시 엔진
│   │   ├── ProcessMatcher.cs       # 프로세스 정밀 매칭
│   │   └── RestartTracker.cs       # 재시작 추적
│   ├── UI/
│   │   ├── Forms/                  # MainStatusForm, SettingsForm, LogViewerForm, ProcessPickerForm
│   │   └── TrayApplicationContext.cs
│   ├── Service/
│   │   └── WatchDogWindowsService.cs
│   └── Program.cs                  # 진입점
├── InterfaceWatchDog.Tests/        # 단위/통합 테스트
├── Tools/ShowSettings/             # 설정 확인 유틸리티
├── Installer/                      # Inno Setup 인스톨러
├── build_installer.ps1             # 인스톨러 빌드 스크립트
└── build_patch.ps1                 # 패치 빌드 스크립트
```

## 설정

설정 파일은 `%ProgramData%\InterfaceWatchDog\` 경로에 저장됩니다.

### config.json

```json
{
  "Erweka": {
    "DisplayName": "ERWEKA Export Manager",
    "ProcessName": "javaw",
    "Arguments": "",
    "ProcessCheckSeconds": 30,
    "Port": 0
  },
  "TabmachineIF": {
    "DisplayName": "TabmachineIF",
    "ProcessName": "TabmachineIF",
    "ExecutablePath": "",
    "Arguments": "",
    "MaxRestartAttempts": 3,
    "RestartCooldownSeconds": 60,
    "ProcessCheckSeconds": 30
  },
  "PdfFolder": {
    "Visible": false,
    "Path": "",
    "MaxIdleMinutes": 30,
    "MaxBacklogCount": 50,
    "FileActivityCheckMinutes": 5
  },
  "DbConnectionVerified": false
}
```

### dbconfig.json

```json
{
  "Server": "서버주소",
  "Database": "데이터베이스명",
  "UserId": "사용자ID",
  "Password": "비밀번호",
  "PlantCode": "공장코드"
}
```

| 항목 | 설명 |
|------|------|
| `Erweka.Port` | TCP 포트 감시 (0 = 비활성) |
| `TabmachineIF.ExecutablePath` | 자동 재시작에 필요한 실행 파일 경로 |
| `PdfFolder.Visible` | PDF 폴더 감시 UI 표시 여부 |
| `PdfFolder.MaxIdleMinutes` | 신규 파일 미생성 허용 시간 (분) |
| `PdfFolder.MaxBacklogCount` | 미처리 파일 수 임계치 |
| `DbConnectionVerified` | DB 연결 검증 완료 여부 |

## 빌드

### 요구 사항
- .NET 8.0 SDK
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (인스톨러 빌드 시)

### 실행 파일 빌드

```powershell
dotnet publish InterfaceWatchDog/InterfaceWatchDog.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:EnableCompressionInSingleFile=true `
  -o InterfaceWatchDog/bin/publish/win-x64
```

### 인스톨러 빌드

```powershell
.\build_installer.ps1
```

`Installer/Output/` 폴더에 `InterfaceWatchDog_Setup_v1.2.1.exe`가 생성됩니다.

### 패치 빌드

```powershell
.\build_patch.ps1
```

## 테스트

```powershell
dotnet test InterfaceWatchDog.Tests/InterfaceWatchDog.Tests.csproj
```

- **프레임워크**: xUnit
- **어설션**: FluentAssertions
- **모킹**: NSubstitute

## 의존성

| 패키지 | 버전 | 용도 |
|--------|------|------|
| Microsoft.Data.SqlClient | 7.0.1 | SQL Server 연결 |
| Serilog | 4.1.0 | 구조적 로깅 |
| Serilog.Sinks.File | 6.0.0 | 파일 로그 출력 |
| System.ServiceProcess.ServiceController | 8.0.0 | Windows 서비스 관리 |
| System.Management | 8.0.0 | WMI 프로세스 조회 |

## 라이선스

이 프로젝트는 사내 전용 소프트웨어입니다.
