# InterfaceWatchDog 개발자 인수인계 문서

> **버전**: v1.4.0 | **플랫폼**: .NET 8.0 / Windows Forms / win-x64 | **작성일**: 2026-06-24

---

## 1. 프로그램 개요

**InterfaceWatchDog**는 한올바이오파마 공장의 두 인터페이스 프로세스를 실시간 감시하는 Windows 모니터링 프로그램이다.

| 감시 대상 | 설명 | 감시 방법 |
|-----------|------|-----------|
| **ERWEKA Export Manager** | ERWEKA 장비 데이터 수출 프로그램 (Java 기반) | 프로세스 존재 + TCP 포트 LISTEN 확인 |
| **TabmachineIF** | 타정기 인터페이스 프로그램 | 프로세스 존재 확인 + 자동 재시작 |

### 주요 동작
- 설정된 주기(기본 30초)로 대상 프로세스 상태 확인
- TabmachineIF 장애 시 자동 재시작 (최대 3회, 쿨다운 60초)
- 장애 지속 시 SQL Server `SYS_ALARM` 테이블에 알람 기록
- 시스템 트레이 아이콘으로 상태 표시 (녹색/주황/파랑/빨강/회색)
- 선택적 PDF 폴더 파일 활동 감시 (유휴/백로그 경고)

---

## 2. 시스템 아키텍처

### Dual-mode 구조

프로그램은 **감시프로그램**과 **Windows 서비스** 두 가지 모드로 실행된다.

```
┌─────────────────────────────────────────────────────────┐
│  사용자 세션 (Session 1+)                                │
│  ┌──────────────────────────────────────┐                │
│  │  감시프로그램 (Program.cs)              │                │
│  │  ├─ WatchDogEngine (감시 + 재시작)   │                │
│  │  ├─ TrayApplicationContext (트레이)   │                │
│  │  └─ MainStatusForm (대시보드)        │                │
│  └──────────────────────────────────────┘                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  Session 0 (비대화형)                                    │
│  ┌──────────────────────────────────────┐                │
│  │  Windows 서비스                       │                │
│  │  ├─ WatchDogEngine (감시만, 재시작 X) │                │
│  │  └─ FileSystemWatcher (설정 핫리로드) │                │
│  └──────────────────────────────────────┘                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  공유 리소스                                             │
│  ├─ %ProgramData%\InterfaceWatchDog\config.json         │
│  ├─ %ProgramData%\InterfaceWatchDog\dbconfig.json       │
│  └─ %ProgramData%\InterfaceWatchDog\Logs\               │
└─────────────────────────────────────────────────────────┘
```

**Session 0 제약**: Windows 서비스는 Session 0에서 실행되어 사용자 데스크톱과 상호작용할 수 없다.
ERWEKA/TabmachineIF 모두 재시작 후 사용자가 화면에서 조작해야 하므로, 서비스 모드에서는 **감시만** 수행하고 **재시작은 감시프로그램**이 전담한다.

### 운영 모드 비교

| 항목 | 감시프로그램 | Windows 서비스 |
|------|-----------|----------------|
| 프로세스 감시 | O | O |
| 자동 재시작 | O | X |
| UI 대시보드 | O | X |
| 부팅 시 자동 시작 | X (로그인 필요) | O |
| 설정 핫리로드 | X | O (FileSystemWatcher) |

**권장 운영**: 서비스 + 감시프로그램 동시 사용. 서비스가 부팅 시 감시를 시작하고, 사용자 로그인 후 감시프로그램이 재시작/UI를 담당.

---

## 3. 코드 구조 및 핵심 클래스

```
InterfaceWatchDog/
├── Program.cs                      # 진입점: --install/--uninstall/서비스/트레이 분기
├── Core/
│   ├── WatchDogEngine.cs           # 핵심 감시 엔진 (3개 타이머 루프)
│   ├── ConfigManager.cs            # JSON 설정 파일 Load/Save
│   ├── ProcessMatcher.cs           # WMI 기반 프로세스 정밀 매칭
│   ├── RestartTracker.cs           # 재시작 시도 횟수/쿨다운 추적
│   ├── Actions/
│   │   ├── ProcessRestarter.cs     # 프로세스 Kill + Start
│   │   ├── AlarmWriter.cs          # SYS_ALARM 테이블 INSERT
│   │   └── LogWriter.cs            # 일별 로그 파일 기록
│   ├── Models/
│   │   ├── AppConfig.cs            # 설정 모델 (Erweka/Tabmachine/PdfFolder/AlarmDb)
│   │   ├── ProgramStatus.cs        # 상태 모델 (HealthStatus enum)
│   │   └── LogEntry.cs             # 로그 항목 모델
│   └── Monitors/
│       ├── ProcessMonitor.cs       # IsRunning/IsPortListening/GetProcessInfo
│       └── FileActivityMonitor.cs  # PDF 폴더 유휴/백로그 감지
├── UI/
│   ├── TrayApplicationContext.cs   # 시스템 트레이 통합 (아이콘/메뉴/풍선알림)
│   └── Forms/
│       ├── MainStatusForm.cs       # 상태 대시보드
│       ├── SettingsForm.cs         # 설정 화면 (3탭)
│       ├── LogViewerForm.cs        # 달력 기반 로그 뷰어
│       └── ProcessPickerForm.cs    # 실행 중 프로세스 선택기
└── Service/
    └── WatchDogWindowsService.cs   # Windows 서비스 (핫리로드)
```

### 핵심 클래스 역할

| 클래스 | 역할 | 비고 |
|--------|------|------|
| `WatchDogEngine` | 감시 오케스트레이션. 3개 타이머로 ERWEKA/TabmachineIF/PDF폴더 독립 점검 | 생성자 2개: 프로덕션(구체 클래스 직접 생성) / 테스트(인터페이스 주입) |
| `ProcessMatcher` | 프로세스 이름 + 실행 경로 + 명령줄 인수로 정밀 매칭 | WMI(`Win32_Process`)로 명령줄 조회. javaw.exe처럼 동일 이름 프로세스 구별 시 필수 |
| `RestartTracker` | 연속 실패 횟수 카운트, 쿨다운 시간 관리 | `internal` 클래스, `InternalsVisibleTo`로 테스트 접근 |
| `ConfigManager` | `%ProgramData%\InterfaceWatchDog\` 경로에 JSON 파일 읽기/쓰기 | `IsFirstRun()` = config.json 파일 존재 여부 |
| `AlarmWriter` | SQL Server `SYS_ALARM` 테이블에 비동기 INSERT | `Task.Run`으로 감시 루프 블로킹 방지 |
| `ProcessMonitor` | `ProcessMatcher.Find()`로 프로세스 존재 확인, `IPGlobalProperties`로 TCP 포트 확인 | |
| `WatchDogWindowsService` | `ServiceBase` 상속, `sc.exe`로 설치/제거, `FileSystemWatcher`로 설정 핫리로드 | 200ms 디바운스로 중복 이벤트 방지 |

---

## 4. 핵심 로직 상세

### 4.1 진입점 분기 (`Program.cs`)

```
Main(args)
  ├─ --install  → WatchDogWindowsService.Install() → sc.exe create/start
  ├─ --uninstall → WatchDogWindowsService.Uninstall() → sc.exe stop/delete
  ├─ !UserInteractive → ServiceBase.Run(WatchDogWindowsService)
  └─ UserInteractive (감시프로그램)
       ├─ 단일 인스턴스 가드 (Global 뮤텍스) → 이미 실행 중이면 안내 후 종료
       ├─ ConfigManager.IsFirstRun() → SettingsForm (초기 설정)
       ├─ WatchDogEngine.Start()
       └─ Application.Run(TrayApplicationContext)
```

> 단일 인스턴스: `Global\InterfaceWatchDog.Tray.SingleInstance` 명명 뮤텍스로 서버 전체 1개만
> 허용한다. 트레이 앱 모드에만 적용되며 서비스 모드는 위에서 분기되어 영향받지 않는다
> (서비스 1개 + 트레이 앱 1개 동시 구동은 그대로 유지).

### 4.2 감시 흐름 (`WatchDogEngine`)

엔진은 3개의 독립 `System.Threading.Timer`를 운영한다:

| 타이머 | 대상 | 기본 주기 | 메서드 |
|--------|------|-----------|--------|
| `_erwekaTimer` | ERWEKA | 30초 | `CheckErweka()` → `CheckErwekaProgram()` |
| `_tabmachineTimer` | TabmachineIF | 30초 | `CheckTabmachine()` → `CheckProgram()` |
| `_fileTimer` | PDF 폴더 | 5분 | `CheckFileActivity()` |

### 4.3 ERWEKA 감시 로직

```
CheckErwekaProgram():
  프로세스 미설정 → Disabled
  프로세스 실행 중 + 포트 OK → Healthy (알람/연속 미감지 카운터 리셋)
  프로세스 미감지 또는 포트 실패:
    ├─ _erwekaConsecutiveMisses++ (연속 미감지 누적)
    ├─ 상태는 즉시 Failed 반영 (UI)
    ├─ 비대화형(서비스) 인스턴스 → 기록 안 함 (중복 방지) 후 종료
    ├─ FailureGraceCount 미만 → 알람 보류, 추적 로그만 ("확인 중 (n/N)")
    └─ FailureGraceCount 도달 → 알람 기록 (1회) + SYS_ALARM
```

- **디바운스**: `ErwekaConfig.FailureGraceCount`(기본 2, 1=즉시) 회 **연속** 미감지 시에만
  알람. 중간에 한 번이라도 Healthy면 카운터 리셋 → 일시적 WMI 오탐으로 인한 거짓 알람 방지.
- **기록 전담**: 알람/로그/SYS_ALARM 기록은 **대화형 인스턴스(`_isInteractiveSession`)만**
  수행 → 트레이 앱·서비스가 동시 구동돼도 중복 기록되지 않음.
- ERWEKA는 **자동 재시작 없음** — Java 기반 프로그램이라 Session 0에서 재시작해도 무용지물.

### 4.4 TabmachineIF 재시작 상태 머신

```
CheckProgram():
  프로세스 미설정 → Disabled
  프로세스 실행 중 → Healthy (실패 카운터 리셋, 알람 플래그 리셋)
  프로세스 미감지:
    ├─ 서비스 모드(비대화형) → Warning 표시만 (재시작 안 함)
    ├─ 쿨다운 중 → 스킵
    ├─ 실패 횟수 ≥ MaxRestartAttempts → Failed 상태 (쿨다운 설정)
    └─ 실패 횟수 < MaxRestartAttempts:
         ├─ IncrementFailure()
         ├─ ProcessRestarter.TryRestart()
         ├─ 성공 → Healthy, 실패 카운터 리셋
         └─ 실패 → Warning/Failed
              └─ 최종 실패 시 → SYS_ALARM 알람 기록 (1회)
```

### 4.5 알람 중복 방지

- `_erwekaAlarmSent` / `_tabAlarmSent` volatile 플래그로 상태 전환 시에만 알람 발송
- 프로세스가 복구(Healthy)되면 플래그 리셋 → 재장애 시 새 알람 발송 가능
- ERWEKA는 `FailureGraceCount` 디바운스(연속 미감지 카운터 `_erwekaConsecutiveMisses`)로
  그레이스 도달 후에만 발송, 미만이면 알람 보류
- 기록은 대화형 인스턴스 전담 → 서비스와 동시 구동 시 중복 기록 방지

### 4.6 설정 핫리로드 (Windows 서비스)

```
FileSystemWatcher (config.json / dbconfig.json 변경 감시)
  → OnConfigFileChanged()
    ├─ LastWriteTime 비교로 중복 이벤트 필터링
    ├─ 200ms 대기 (파일 쓰기 완료 대기)
    └─ engine.ReloadConfig() → 타이머 주기 즉시 반영
```

---

## 5. 설정 관리

### 설정 파일 위치

| 파일 | 경로 | 내용 |
|------|------|------|
| `config.json` | `%ProgramData%\InterfaceWatchDog\config.json` | 감시 대상 설정 |
| `dbconfig.json` | `%ProgramData%\InterfaceWatchDog\dbconfig.json` | DB 연결 정보 |

> `%ProgramData%` = 일반적으로 `C:\ProgramData`

### config.json 주요 항목

```json
{
  "Erweka": {
    "DisplayName": "ERWEKA Export Manager",
    "ProcessName": "javaw",           // 프로세스 이름 (확장자 제외)
    "Arguments": "",                   // 명령줄 매칭 문자열 (javaw 구분용)
    "ProcessCheckSeconds": 30,         // 감시 주기 (10~300초)
    "Port": 0                          // TCP 포트 감시 (0=비활성)
  },
  "TabmachineIF": {
    "DisplayName": "TabmachineIF",
    "ProcessName": "TabmachineIF",
    "ExecutablePath": "",              // 자동 재시작에 필요한 exe 절대 경로
    "Arguments": "",                   // 실행 시 전달할 인수
    "MaxRestartAttempts": 3,           // 연속 재시작 최대 횟수 (1~10)
    "RestartCooldownSeconds": 60,      // 재시작 간 대기 시간
    "ProcessCheckSeconds": 30
  },
  "PdfFolder": {
    "Visible": false,                  // PDF 감시 UI 표시 여부
    "Path": "",                        // 감시할 폴더 경로
    "MaxIdleMinutes": 30,              // 유휴 경고 임계치 (분)
    "MaxBacklogCount": 50,             // 백로그 경고 임계치 (파일 수)
    "FileActivityCheckMinutes": 5      // 감시 주기 (분)
  },
  "DbConnectionVerified": false        // DB 연결 검증 완료 여부
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

- `ConnectionString`은 `AlarmDbConfig` 클래스에서 자동 생성
- `TrustServerCertificate=True`가 기본 포함됨

---

## 6. 빌드 및 배포

### 빌드 환경 요구사항

- .NET 8.0 SDK
- Inno Setup 6 (인스톨러 빌드 시) — https://jrsoftware.org/isinfo.php
- PowerShell 5.1+

### 실행 파일 빌드

```powershell
dotnet publish InterfaceWatchDog/InterfaceWatchDog.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:EnableCompressionInSingleFile=true `
  -o InterfaceWatchDog/bin/publish/win-x64
```

- 결과: 단일 exe 파일 (약 75MB, .NET 런타임 포함)
- `--self-contained`: 대상 PC에 .NET 설치 불필요

### 인스톨러 빌드

```powershell
.\build_installer.ps1                # dotnet publish + Inno Setup 컴파일
.\build_installer.ps1 -SkipPublish   # 기존 publish 결과 재사용
```

- 출력: `Installer/Output/InterfaceWatchDog_Setup_v{버전}.exe`
- 버전은 `.csproj`의 `<Version>` 태그에서 자동 추출
- Inno Setup 스크립트: `Installer/setup.iss`

### 패치 배포 (서비스 중단 없이 업데이트)

```powershell
.\build_patch.ps1                    # publish + 패치 폴더 생성
.\build_patch.ps1 -SkipPublish       # 기존 publish 결과 재사용
```

- 출력: `Installer/Patch/InterfaceWatchDog.exe` + `apply_patch.ps1`
- 현장 적용:
  ```powershell
  # 관리자 권한 필요
  .\Installer\Patch\apply_patch.ps1
  ```
- 동작: 서비스 중지 → 감시프로그램 종료 → exe 교체 → 서비스 재시작

### 설치 구조

- 설치 경로: `C:\Program Files\InterfaceWatchDog\`
- 레지스트리: `HKLM\SOFTWARE\InterfaceWatchDog\InterfaceWatchDog` (InstallPath, Version, ConfigPath)
- 데이터 경로: `C:\ProgramData\InterfaceWatchDog\` (config, logs)

---

## 7. 테스트

### 실행 방법

```powershell
dotnet test InterfaceWatchDog.Tests/InterfaceWatchDog.Tests.csproj
```

### 테스트 스택

| 라이브러리 | 용도 |
|-----------|------|
| xUnit | 테스트 프레임워크 |
| FluentAssertions | 어설션 |
| NSubstitute | 모킹 |

### 테스트 가능 구조

핵심 클래스는 인터페이스 기반으로 설계되어 모킹이 용이하다:

| 인터페이스 | 구현 클래스 | 역할 |
|-----------|------------|------|
| `IProcessMonitor` | `ProcessMonitor` | 프로세스/포트 상태 조회 |
| `IProcessRestarter` | `ProcessRestarter` | 프로세스 재시작 |
| `IAlarmWriter` | `AlarmWriter` | DB 알람 기록 |
| `IFileActivityMonitor` | `FileActivityMonitor` | 파일 활동 감시 |

`WatchDogEngine`의 `internal` 테스트 생성자로 모든 의존성을 주입할 수 있다:

```csharp
// 테스트 예시
var engine = new WatchDogEngine(config, log,
    processMonitor: Substitute.For<IProcessMonitor>(),
    fileMonitor: Substitute.For<IFileActivityMonitor>(),
    restarter: Substitute.For<IProcessRestarter>(),
    alarmWriter: Substitute.For<IAlarmWriter>());
```

### 테스트 범위

- `Engine/`: WatchDogEngine 통합 시나리오 (정상/장애/재시작/알람)
- `Models/`: AppConfig 직렬화, ProgramStatus 상태 전환
- `Actions/`: LogWriter, ProcessRestarter
- `Monitors/`: ProcessMonitor, FileActivityMonitor
- `Core/`: ConfigManager, RestartTracker
- `Integration/`: DB 알람 기록, 설정 파일 영속성

---

## 8. 유지보수 가이드

### 8.1 감시 대상 추가 시 수정 포인트

현재 ERWEKA/TabmachineIF 2개 고정 구조. 새 대상 추가 시:

1. `AppConfig.cs` — 새 Config 클래스 추가
2. `WatchDogEngine.cs` — 새 타이머 + Check 메서드 추가
3. `MainStatusForm.cs` — 상태 카드 UI 추가
4. `SettingsForm.cs` — 설정 탭 추가
5. `config.json` — 새 섹션 추가

### 8.2 SYS_ALARM 테이블 스키마

```sql
-- AlarmWriter.cs에서 사용하는 INSERT 구문
INSERT INTO SYS_ALARM
    (PLANT_CD, ALARM_CONTENT, READ_YN, FORM_NAME,
     REF_KEY1, REF_KEY2, INSERT_USER_ID, INSERT_TIME, REMARK)
VALUES
    (@PlantCd, @Content, 'N', 'InterfaceWatchDog',
     @ErrorDate, @ProcessName, 'SYSTEM', @InsertTime, 'I/F감시프로그램')
```

| 컬럼 | 값 | 설명 |
|------|-----|------|
| `PLANT_CD` | dbconfig의 PlantCode | 공장 코드 |
| `ALARM_CONTENT` | 장애 메시지 | 예: "ERWEKA Export Manager 프로세스 미감지" |
| `READ_YN` | 'N' | 읽음 여부 (초기값 N) |
| `FORM_NAME` | 'InterfaceWatchDog' | 발생 프로그램 식별 |
| `REF_KEY1` | 에러 발생 시각 | yyyy-MM-dd HH:mm:ss |
| `REF_KEY2` | 프로세스 이름 | 예: "javaw", "TabmachineIF" |
| `INSERT_USER_ID` | 'SYSTEM' | 고정값 |
| `REMARK` | 'I/F감시프로그램' | 고정값 |

### 8.3 로그 관리

- 경로: `%ProgramData%\InterfaceWatchDog\Logs\`
- 파일명: `watchdog_YYYY-MM-DD.log`
- 형식: `[YYYY-MM-DD HH:MM:SS] [LEVEL] [Source] Message`
- 자동 정리 기능 없음 — 필요 시 오래된 로그 수동 삭제

### 8.4 Windows 서비스 명령어

```powershell
# 서비스 설치 (앱 내부에서)
InterfaceWatchDog.exe --install

# 서비스 제거
InterfaceWatchDog.exe --uninstall

# 수동 관리
sc start InterfaceWatchDog
sc stop InterfaceWatchDog
sc query InterfaceWatchDog
```

### 8.5 자주 발생하는 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| ERWEKA 감시가 Disabled | config.json의 `Erweka.ProcessName`이 비어있음 | 설정 화면에서 ERWEKA 프로세스 선택 |
| TabmachineIF 재시작 실패 | `ExecutablePath`가 잘못되었거나 파일이 없음 | 설정에서 올바른 exe 경로 지정 |
| 알람이 DB에 기록되지 않음 | dbconfig.json 미설정 또는 `DbConnectionVerified = false` | 설정 화면에서 DB 연결 테스트 |
| 서비스에서 재시작이 안 됨 | 정상 동작 — Session 0 제약으로 의도적 비활성화 | 감시프로그램을 함께 실행 |
| javaw 프로세스 구분 실패 | `Erweka.Arguments`가 비어있어 모든 javaw를 매칭 | Arguments에 ERWEKA 고유 문자열 설정 |
| 서비스 시작 후 감시 안 됨 | config.json이 없는 상태에서 서비스 시작 | 감시프로그램에서 초기 설정 완료 후 서비스 재시작 |

---

## 9. 의존성 및 환경

### NuGet 패키지

| 패키지 | 버전 | 용도 |
|--------|------|------|
| Microsoft.Data.SqlClient | 7.0.1 | SQL Server 연결 (SYS_ALARM 알람 기록) |
| Serilog | 4.1.0 | 구조적 로깅 (패키지 참조만 있고 실제 사용은 커스텀 LogWriter) |
| Serilog.Sinks.File | 6.0.0 | 위와 동일 |
| System.ServiceProcess.ServiceController | 8.0.0 | Windows 서비스 관리 API |
| System.Management | 8.0.0 | WMI 프로세스 명령줄 조회 |

### 프로젝트 구성

| 프로젝트 | 유형 | 설명 |
|---------|------|------|
| `InterfaceWatchDog` | WinExe (.NET 8.0) | 메인 애플리케이션 |
| `InterfaceWatchDog.Tests` | xUnit 테스트 | 단위/통합 테스트 |
| `Tools/PptGenerator` | 콘솔 앱 | 사용법 PPT 자동 생성 (OpenXml) |
| `Tools/ShowSettings` | 콘솔 앱 | 현재 설정 표시 유틸리티 |

### 추가 도구

| 파일 | 용도 |
|------|------|
| `build_installer.ps1` | dotnet publish + Inno Setup 인스톨러 빌드 |
| `build_patch.ps1` | 패치 exe + 적용 스크립트 생성 |
| `Installer/apply_patch.ps1` | 현장 패치 적용 (서비스 중지→교체→재시작) |
| `Tools/review.ps1` | 코드 리뷰용 diff 수집 스크립트 |

---

## 부록: 버전 이력

| 버전 | 주요 변경 |
|------|-----------|
| v1.0.0 | 초기 릴리스 |
| v1.1.0 | 패치 배포 스크립트 추가 |
| v1.2.0 | config/dbconfig 분리, PDF 감시 활성화 플래그, UI 개선 |
| v1.2.1 | TabmachineIF 재시작 실패 시 SYS_ALARM 기록 추가 |
| v1.3.0 | 트레이 아이콘 방패 형태 개선, 기본 사용법 PPT 추가 |
| v1.4.0 | ERWEKA WMI 오탐 수정, 미감지 디바운스(`FailureGraceCount`), LOG 중복 알람 제거, 트레이앱 중복 실행 방지(단일 인스턴스) |
