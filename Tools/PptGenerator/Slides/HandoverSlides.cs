using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;

namespace PptGenerator.Slides;

public static class HandoverSlides
{
    private const string AccentColor = "1C2028";

    public static void AddCoverSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.SetSlideBackground(slide, AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 6, SlideBuilder.EmuCm / 6,
            "2563EB"));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 6,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 4,
            "InterfaceWatchDog",
            fontSize: 4000, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Top));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "개발자 인수인계",
            fontSize: 3200, fontColor: "8C94A5",
            anchor: D.TextAnchoringTypeValues.Top));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 12,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "ERWEKA / TabmachineIF 인터페이스 감시 시스템  |  v1.4.0",
            fontSize: 1600, fontColor: "6B7280",
            anchor: D.TextAnchoringTypeValues.Top));

        SlideBuilder.AddNotes(slide, """
            이 프로그램은 공장에서 사용하는 두 가지 인터페이스 프로그램이 정상적으로 동작하는지 감시하는 프로그램입니다.

            ERWEKA Export Manager: 장비에서 측정 데이터를 내보내는 Java 기반 프로그램입니다.
            TabmachineIF: ERWEKA에서 전달받은 PDF 파일을 처리하는 인터페이스 프로그램입니다.

            이 두 프로그램이 갑자기 꺼지면 생산 데이터가 유실될 수 있기 때문에,
            InterfaceWatchDog가 주기적으로 확인하고 문제가 생기면 자동으로 재시작하거나 알람을 보냅니다.

            이 PPT는 이 프로그램을 앞으로 유지보수할 개발자를 위한 기술 인수인계 자료입니다.
            """);
    }

    public static void AddSectionTitle(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.SetSlideBackground(slide, AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 3,
            "개발자 인수인계",
            fontSize: 3600, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Center,
            align: D.TextAlignmentTypeValues.Center));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "아키텍처, 코드 구조, 핵심 로직, 빌드/배포, 유지보수",
            fontSize: 2000, fontColor: "8C94A5",
            align: D.TextAlignmentTypeValues.Center));

        SlideBuilder.AddNotes(slide, """
            이 섹션에서는 다음 5가지를 다룹니다.

            1. 시스템 아키텍처: 프로그램이 어떤 구조로 되어 있는지 큰 그림을 봅니다.
            2. 코드 구조: 소스 코드 폴더와 파일이 어떻게 나뉘어 있는지, 수정할 때 어디를 봐야 하는지 알려드립니다.
            3. 핵심 로직: 감시 → 장애 감지 → 재시작 → 알람까지의 동작 흐름을 설명합니다.
            4. 빌드/배포: 소스 코드를 실행 파일로 만들고 현장에 배포하는 방법입니다.
            5. 유지보수: 운영 중 자주 확인하게 될 경로와 새 기능 추가 시 수정해야 할 파일들입니다.

            상세 내용은 Docs/HANDOVER.md 문서에도 정리되어 있으니 함께 참고하세요.
            """);
    }

    public static void AddArchitectureSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "시스템 아키텍처 (Dual-mode)", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Tray App box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 7,
            "EBF5FF"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "감시프로그램 (Session 1+)", fontSize: 1600, fontColor: "2563EB", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 5,
            new[]
            {
                "WatchDogEngine (감시 + 재시작)",
                "TrayApplicationContext (트레이 아이콘)",
                "MainStatusForm (대시보드)",
                "SettingsForm (설정 UI)",
                "사용자 로그인 시 실행"
            },
            fontSize: 1300, fontColor: "374151"));

        // Windows Service box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 7,
            "FEF3C7"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "Windows 서비스 (Session 0)", fontSize: 1600, fontColor: "D97706", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 5,
            new[]
            {
                "WatchDogEngine (감시만, 재시작 X)",
                "FileSystemWatcher (설정 핫리로드)",
                "부팅 시 자동 시작",
                "Session 0 제약: UI 불가",
                "재시작은 감시프로그램이 전담"
            },
            fontSize: 1300, fontColor: "374151"));

        // Shared resources box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 11),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 3,
            "F0FDF4"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 11.3),
            SlideBuilder.EmuCm * 28, SlideBuilder.EmuCm * 1,
            "공유 리소스 (%ProgramData%\\InterfaceWatchDog\\)", fontSize: 1500, fontColor: "16A34A", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 12.5),
            SlideBuilder.EmuCm * 28, SlideBuilder.EmuCm * 2,
            new[]
            {
                "config.json (감시 설정)     dbconfig.json (DB 연결)     Logs\\ (일별 로그)"
            },
            fontSize: 1300, fontColor: "374151"));

        // Key insight
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 15),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 3,
            "핵심: ERWEKA/TabmachineIF 모두 재시작 후 사용자 조작이 필요하므로, Session 0(서비스)에서는 재시작이 무용지물 → 감시프로그램이 재시작 전담",
            fontSize: 1400, fontColor: "DC2626", bold: true));

        SlideBuilder.AddNotes(slide, """
            이 프로그램은 두 가지 모드로 실행될 수 있습니다.

            [감시프로그램 모드]
            사용자가 Windows에 로그인한 상태에서 실행됩니다.
            화면 오른쪽 아래 시스템 트레이에 아이콘이 표시되고, 대시보드도 볼 수 있습니다.
            가장 중요한 점은 프로세스가 죽었을 때 "자동 재시작"을 할 수 있다는 것입니다.

            [Windows 서비스 모드]
            PC가 켜지면 아무도 로그인하지 않아도 자동으로 시작됩니다.
            하지만 "Session 0"이라는 특별한 영역에서 실행되기 때문에 화면에 아무것도 표시할 수 없고,
            다른 프로그램을 실행시켜도 사용자 화면에 나타나지 않습니다.
            그래서 감시만 하고, 재시작은 하지 않습니다.

            [왜 둘 다 필요한가?]
            서비스가 PC 부팅 시 바로 감시를 시작하고,
            사용자가 로그인하면 감시프로그램이 실제 재시작을 처리합니다.
            두 모드는 같은 설정 파일(config.json, dbconfig.json)을 공유합니다.

            [공유 리소스 경로]
            설정과 로그는 C:\ProgramData\InterfaceWatchDog\ 에 저장됩니다.
            이 경로는 모든 사용자와 서비스가 접근할 수 있는 공용 폴더입니다.
            """);
    }

    public static void AddCodeStructureSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "코드 구조", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Directory structure
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 14,
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "디렉토리 구조", fontSize: 1600, fontColor: AccentColor, bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 12,
            new[]
            {
                "Program.cs — 진입점",
                "Core/WatchDogEngine.cs — 감시 엔진",
                "Core/ConfigManager.cs — 설정 관리",
                "Core/ProcessMatcher.cs — 프로세스 매칭",
                "Core/RestartTracker.cs — 재시작 추적",
                "Core/Actions/ — 실행 액션",
                "  ProcessRestarter, AlarmWriter, LogWriter",
                "Core/Models/ — 데이터 모델",
                "  AppConfig, ProgramStatus, LogEntry",
                "Core/Monitors/ — 감시 모듈",
                "  ProcessMonitor, FileActivityMonitor",
                "UI/ — 사용자 인터페이스",
                "Service/ — Windows 서비스"
            },
            fontSize: 1200, fontColor: "374151"));

        // Key classes table
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 18, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 1,
            "핵심 클래스", fontSize: 1600, fontColor: AccentColor, bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 14,
            new[] { "클래스", "역할" },
            new[]
            {
                new[] { "WatchDogEngine", "3개 타이머 감시 루프" },
                new[] { "ProcessMatcher", "WMI 명령줄 매칭" },
                new[] { "RestartTracker", "실패 횟수/쿨다운" },
                new[] { "ProcessRestarter", "Kill + Start" },
                new[] { "AlarmWriter", "SYS_ALARM INSERT" },
                new[] { "LogWriter", "일별 로그 파일" },
                new[] { "ConfigManager", "JSON Load/Save" }
            },
            rowHeight: 380000, fontSize: 1200);
        tree.Append(table);

        // DI note
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 18, (long)(SlideBuilder.EmuCm * 14),
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 3,
            "테스트: IProcessMonitor, IProcessRestarter, IAlarmWriter, IFileActivityMonitor 인터페이스로 모킹 가능",
            fontSize: 1200, fontColor: "16A34A", bold: true));

        SlideBuilder.AddNotes(slide, """
            소스 코드는 크게 4개 영역으로 나뉩니다.

            [Core 폴더 — 프로그램의 두뇌]
            WatchDogEngine.cs가 가장 핵심입니다. 타이머 3개를 돌려서 ERWEKA, TabmachineIF, PDF 폴더를 각각 독립적으로 감시합니다.
            ProcessMatcher.cs는 "이 프로세스가 진짜 우리가 찾는 프로세스인가?"를 판별합니다.
            예를 들어 javaw.exe가 여러 개 실행 중일 때, 명령줄 인수를 비교해서 ERWEKA만 골라냅니다.
            RestartTracker.cs는 "재시작을 몇 번 시도했나, 쿨다운 시간이 지났나"를 추적합니다.

            [Actions 폴더 — 실제로 뭔가를 하는 클래스들]
            ProcessRestarter: 프로세스를 죽이고 새로 실행합니다.
            AlarmWriter: DB의 SYS_ALARM 테이블에 알람을 기록합니다.
            LogWriter: 매일 새 로그 파일을 만들어 기록합니다.

            [Models 폴더 — 데이터 구조]
            AppConfig: config.json의 내용을 담는 클래스입니다.
            ProgramStatus: 각 프로그램의 현재 상태(정상/경고/재시작중/실패 등)를 담습니다.

            [테스트 관련]
            핵심 클래스들은 인터페이스(IProcessMonitor 등)로 추상화되어 있어서,
            테스트할 때 실제 프로세스나 DB 없이도 가짜 객체(Mock)를 넣어서 테스트할 수 있습니다.
            """);
    }

    public static void AddCoreLogicSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "핵심 로직 흐름", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // ERWEKA flow
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "FEF2F2"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "ERWEKA 감시 (재시작 없음)", fontSize: 1500, fontColor: "DC2626", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 4.8),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "프로세스 실행 + 포트 LISTEN → Healthy",
                "프로세스 미감지 또는 포트 실패 → Failed",
                "Failed 전환 시 SYS_ALARM 1회 기록",
                "복구 시 알람 플래그 리셋"
            },
            fontSize: 1200, fontColor: "374151"));

        // TabmachineIF flow
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "EFF6FF"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "TabmachineIF 감시 (자동 재시작)", fontSize: 1500, fontColor: "2563EB", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 4.8),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "프로세스 실행 중 → Healthy (카운터 리셋)",
                "미감지 + 쿨다운 중 → 스킵",
                "미감지 → 재시작 시도 (최대 N회)",
                "최종 실패 → Failed + SYS_ALARM 기록"
            },
            fontSize: 1200, fontColor: "374151"));

        // State machine
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 10),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "HealthStatus 상태 전이", fontSize: 1600, fontColor: AccentColor, bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 11.5),
            SlideBuilder.EmuCm * 30,
            new[] { "상태", "색상", "의미", "전환 조건" },
            new[]
            {
                new[] { "Healthy", "녹색", "정상 실행 중", "프로세스 감지됨" },
                new[] { "Warning", "주황", "경고/서비스 모드", "서비스에서 미감지" },
                new[] { "Restarting", "파랑", "재시작 시도 중", "재시작 실행 중" },
                new[] { "Failed", "빨강", "복구 실패", "최대 시도 초과" },
                new[] { "Disabled", "회색", "감시 비활성", "프로세스 미설정" }
            },
            rowHeight: 380000, fontSize: 1200);
        tree.Append(table);

        SlideBuilder.AddNotes(slide, """
            프로그램의 감시 동작을 이해하는 가장 중요한 슬라이드입니다.

            [ERWEKA 감시 — 왼쪽 빨간 박스]
            ERWEKA는 Java 프로그램이라 자동 재시작을 하지 않습니다.
            대신 "프로세스가 실행 중인가?" + "TCP 포트가 응답하는가?" 두 가지를 확인합니다.
            둘 중 하나라도 실패하면 바로 Failed 상태가 되고, DB에 알람을 1회 기록합니다.
            나중에 프로세스가 복구되면 알람 플래그가 리셋되어, 다시 장애가 나면 새 알람이 기록됩니다.

            [TabmachineIF 감시 — 오른쪽 파란 박스]
            TabmachineIF는 자동 재시작을 시도합니다. 동작 순서는:
            1) 프로세스가 없으면 재시작 시도 (기본 최대 3회)
            2) 시도 사이에 쿨다운(기본 60초) 대기
            3) 3회 모두 실패하면 Failed 상태 → DB 알람 기록
            4) 프로세스가 살아나면 카운터 리셋 → 다시 정상 감시

            [HealthStatus 상태값]
            코드에서 HealthStatus라는 enum으로 관리됩니다.
            UI에서 트레이 아이콘 색상, 대시보드 상태 카드 색상이 이 값에 따라 바뀝니다.
            가장 나쁜 상태가 트레이 아이콘 색상을 결정합니다 (예: 하나라도 Failed면 빨간색).

            [알람 중복 방지]
            _erwekaAlarmSent, _tabAlarmSent라는 volatile 플래그로 같은 장애에 대해 알람이 반복 기록되지 않도록 합니다.
            """);
    }

    public static void AddBuildDeploySlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "빌드 및 배포", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Build commands
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "빌드 명령어", fontSize: 1600, fontColor: AccentColor, bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 4.5),
            SlideBuilder.EmuCm * 30,
            new[] { "작업", "명령어", "출력" },
            new[]
            {
                new[] { "실행 파일", "dotnet publish -c Release\n-r win-x64 --self-contained", "bin/publish/win-x64/" },
                new[] { "인스톨러", ".\\build_installer.ps1", "Installer/Output/*.exe" },
                new[] { "패치", ".\\build_patch.ps1", "Installer/Patch/" },
                new[] { "테스트", "dotnet test", "xUnit 결과" }
            },
            rowHeight: 500000, fontSize: 1200);
        tree.Append(table);

        // Build requirements
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 11),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "빌드 환경 요구사항", fontSize: 1600, fontColor: AccentColor, bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 12.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 3,
            new[]
            {
                ".NET 8.0 SDK",
                "Inno Setup 6 (인스톨러 빌드 시) — jrsoftware.org",
                "PowerShell 5.1+"
            },
            fontSize: 1300, fontColor: "374151"));

        // Deployment flow
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 15.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "배포: Self-Contained 단일 EXE (~75MB, .NET 런타임 포함) → 대상 PC에 .NET 설치 불필요",
            fontSize: 1400, fontColor: "16A34A", bold: true));

        SlideBuilder.AddNotes(slide, """
            코드를 수정한 뒤 현장에 배포하는 과정입니다.

            [개발 환경 준비]
            .NET 8.0 SDK가 설치되어 있어야 합니다.
            인스톨러를 만들려면 Inno Setup 6도 필요합니다 (무료 프로그램).

            [빌드 방법 — 3가지]
            1) dotnet publish: 소스 코드를 실행 파일(exe)로 변환합니다.
               --self-contained 옵션 덕분에 현장 PC에 .NET을 따로 설치할 필요가 없습니다.
               대신 exe 크기가 약 75MB로 큽니다.

            2) build_installer.ps1: dotnet publish + Inno Setup 컴파일을 한 번에 합니다.
               결과물은 Installer/Output/ 폴더에 Setup exe 파일로 나옵니다.
               신규 설치 시 이 인스톨러를 사용합니다.

            3) build_patch.ps1: 이미 설치된 곳에 exe만 교체할 때 사용합니다.
               Installer/Patch/ 폴더에 exe + 적용 스크립트가 생성됩니다.
               현장에서 apply_patch.ps1을 관리자 권한으로 실행하면
               서비스 중지 → exe 교체 → 서비스 재시작이 자동으로 됩니다.

            [주의사항]
            패치 시 설정 파일(config.json)과 로그는 건드리지 않으므로 안전합니다.
            테스트는 dotnet test 명령으로 실행합니다. 배포 전에 반드시 확인하세요.
            """);
    }

    public static void AddMaintenanceSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "유지보수 포인트", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Key paths
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "주요 경로", fontSize: 1600, fontColor: AccentColor, bold: true));

        var pathTable = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 4.5),
            SlideBuilder.EmuCm * 30,
            new[] { "항목", "경로" },
            new[]
            {
                new[] { "설치 경로", "C:\\Program Files\\InterfaceWatchDog\\" },
                new[] { "감시 설정", "%ProgramData%\\InterfaceWatchDog\\config.json" },
                new[] { "DB 설정", "%ProgramData%\\InterfaceWatchDog\\dbconfig.json" },
                new[] { "로그", "%ProgramData%\\InterfaceWatchDog\\Logs\\watchdog_*.log" },
                new[] { "레지스트리", "HKLM\\SOFTWARE\\InterfaceWatchDog" }
            },
            rowHeight: 370000, fontSize: 1200);
        tree.Append(pathTable);

        // Extension guide
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 11),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "감시 대상 추가 시 수정 포인트", fontSize: 1600, fontColor: AccentColor, bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 12.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 5,
            new[]
            {
                "AppConfig.cs — 새 Config 클래스 추가",
                "WatchDogEngine.cs — 새 타이머 + Check 메서드 추가",
                "MainStatusForm.cs — 상태 카드 UI 추가",
                "SettingsForm.cs — 설정 탭 추가",
                "config.json — 새 섹션 추가"
            },
            fontSize: 1300, fontColor: "374151"));

        // Note
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 17,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "상세 인수인계 문서: Docs/HANDOVER.md 참조",
            fontSize: 1400, fontColor: "2563EB", bold: true));

        SlideBuilder.AddNotes(slide, """
            [주요 경로]
            설정 파일: C:\ProgramData\InterfaceWatchDog\ 아래의 config.json, dbconfig.json
            로그 파일: 같은 경로의 Logs 폴더에 날짜별로 생성됩니다 (watchdog_2026-06-22.log 형식)
            설치 경로: C:\Program Files\InterfaceWatchDog\ (exe 파일 위치)
            로그는 자동 정리 기능이 없으므로, 디스크 용량이 부족하면 오래된 로그를 수동으로 삭제해야 합니다.

            [새 감시 대상을 추가하려면?]
            현재는 ERWEKA와 TabmachineIF 두 가지가 고정으로 들어가 있습니다.
            새 프로그램을 감시하고 싶다면 다음 5개 파일을 수정해야 합니다:
            1) AppConfig.cs — 새 설정 클래스 (이름, 경로, 체크 주기 등)
            2) WatchDogEngine.cs — 새 타이머 + Check 메서드 (감시 로직)
            3) MainStatusForm.cs — 대시보드에 상태 카드 추가
            4) SettingsForm.cs — 설정 화면에 탭 추가
            5) config.json — 새 섹션 추가

            기존 TabmachineIF 구현을 복사해서 수정하는 것이 가장 빠릅니다.

            [더 자세한 내용]
            Docs/HANDOVER.md 파일에 코드 수준의 상세 인수인계 문서가 있습니다.
            이 PPT는 요약본이고, 실제 작업 시에는 HANDOVER.md를 참고하세요.
            """);
    }
}
