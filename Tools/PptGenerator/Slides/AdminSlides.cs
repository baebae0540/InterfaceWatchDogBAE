using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;

namespace PptGenerator.Slides;

public static class AdminSlides
{
    private const string AccentColor = "1C2028";

    public static void AddSectionTitle(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.SetSlideBackground(slide, AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 3,
            "IT 관리자 가이드",
            fontSize: 3600, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Center,
            align: D.TextAlignmentTypeValues.Center));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "서비스 관리, 패치 배포, DB 알람 설정",
            fontSize: 2000, fontColor: "8C94A5",
            align: D.TextAlignmentTypeValues.Center));
    }

    public static void AddServiceSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "Windows 서비스 관리", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // GUI method
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "GUI (감시 프로그램)", fontSize: 1600, fontColor: AccentColor, bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "트레이 아이콘 우클릭",
                "\"Windows 서비스 등록\" 클릭",
                "UAC 관리자 권한 승인",
                "서비스 자동 등록 + 시작"
            },
            fontSize: 1400, fontColor: "374151"));

        // CLI method
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "명령줄 (CMD/PowerShell)", fontSize: 1600, fontColor: AccentColor, bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "등록:  InterfaceWatchDog.exe --install",
                "해제:  InterfaceWatchDog.exe --uninstall",
                "상태:  sc query InterfaceWatchDog",
                "시작:  sc start InterfaceWatchDog",
                "중지:  sc stop InterfaceWatchDog"
            },
            fontSize: 1300, fontColor: "374151"));

        // Key features
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 10,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "서비스 특징", fontSize: 1600, fontColor: "333333", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 11.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 6,
            new[]
            {
                "부팅 시 자동 시작 (Automatic 타입)",
                "설정 파일 변경 시 자동 반영 (FileSystemWatcher) — 서비스 재시작 불필요",
                "config.json이 없으면 대기 상태 — 감시 프로그램에서 초기 설정 필요"
            },
            fontSize: 1400, fontColor: "555555"));
    }

    public static void AddDbAlarmSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "DB 알람 설정 (SYS_ALARM)", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "장애 발생 시 SQL Server SYS_ALARM 테이블에 자동으로 알람을 기록합니다.",
            fontSize: 1500, fontColor: "555555"));

        // Config location
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.8),
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 5.2),
            SlideBuilder.EmuCm * 28, (long)(SlideBuilder.EmuCm * 1.5),
            "설정 파일:  %ProgramData%\\InterfaceWatchDog\\dbconfig.json",
            fontSize: 1400, fontColor: "333333", bold: true,
            fontFamily: "Consolas"));

        // Settings table
        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 7.5),
            SlideBuilder.EmuCm * 30,
            new[] { "항목", "설명", "예시" },
            new[]
            {
                new[] { "Server", "SQL Server 주소", "192.168.1.100" },
                new[] { "Database", "데이터베이스 이름", "MES_DB" },
                new[] { "UserId", "SQL 로그인 사용자", "mes_user" },
                new[] { "Password", "SQL 로그인 비밀번호", "****" },
                new[] { "PlantCode", "공장 코드", "P01" }
            },
            rowHeight: 400000, fontSize: 1300);
        tree.Append(table);

        // Alarm conditions
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 14.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "알람 발생 조건", fontSize: 1500, fontColor: "333333", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 15.8),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 3,
            new[]
            {
                "ERWEKA Export Manager 프로세스 중지 감지",
                "TabmachineIF 설정된 재시작 횟수 모두 소진 후 최종 실패"
            },
            fontSize: 1400, fontColor: "DC2626"));
    }

    public static void AddLogSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "로그 관리", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Log viewer
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "로그 뷰어 사용법", fontSize: 1800, fontColor: "333333", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 4.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 5,
            new[]
            {
                "트레이 아이콘 우클릭 → \"로그 보기\" 클릭",
                "좌측 달력에서 날짜 선택 → 해당 날짜 로그 표시",
                "로그가 있는 날짜: 파란색 강조 표시",
                "오늘 날짜: 빨간색 표시",
                "\"폴더 열기\" 버튼으로 로그 파일 직접 접근"
            },
            fontSize: 1400, fontColor: "555555"));

        // Log level table
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "로그 레벨", fontSize: 1800, fontColor: "333333", bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 10.5),
            SlideBuilder.EmuCm * 30,
            new[] { "레벨", "색상", "의미" },
            new[]
            {
                new[] { "INFO", "흰색", "정상 동작 로그 (감시 시작, 프로세스 감지 등)" },
                new[] { "WARN", "주황색", "경고 (프로세스 미감지, 재시작 시도 등)" },
                new[] { "ERROR", "빨간색", "오류 (재시작 실패, DB 기록 실패 등)" }
            },
            rowHeight: 450000, fontSize: 1300);
        tree.Append(table);

        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 15.5),
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.8),
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 15.7),
            SlideBuilder.EmuCm * 28, (long)(SlideBuilder.EmuCm * 1.5),
            "로그 파일:  %ProgramData%\\InterfaceWatchDog\\Logs\\watchdog_YYYY-MM-DD.log",
            fontSize: 1400, fontColor: "333333", bold: true,
            fontFamily: "Consolas"));
    }

    public static void AddPatchSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "패치 배포", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "서비스 중단 없이 실행 파일만 교체하여 업데이트할 수 있습니다.",
            fontSize: 1500, fontColor: "555555"));

        // Patch files
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "패치 파일 구성", fontSize: 1600, fontColor: "333333", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 6.3),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            new[]
            {
                "InterfaceWatchDog.exe  (업데이트된 실행 파일)",
                "apply_patch.ps1  (자동 배포 스크립트)"
            },
            fontSize: 1400, fontColor: "555555"));

        // Steps
        var steps = new[]
        {
            ("1", "관리자 PowerShell 열기", "시작 → PowerShell → 관리자 권한으로 실행"),
            ("2", "패치 폴더로 이동", "cd <패치 파일 경로>"),
            ("3", "스크립트 실행", ".\\apply_patch.ps1"),
            ("4", "자동 처리", "서비스 중지 → EXE 교체 → 서비스 재시작 (자동)")
        };

        for (int i = 0; i < steps.Length; i++)
        {
            var (num, title, desc) = steps[i];
            var boxY = (long)(SlideBuilder.EmuCm * (9 + i * 2));

            tree.Append(SlideBuilder.CreateFilledRect(
                SlideBuilder.EmuCm * 2, boxY, (long)(SlideBuilder.EmuCm * 1.2), (long)(SlideBuilder.EmuCm * 1.2), AccentColor));
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 2, boxY, (long)(SlideBuilder.EmuCm * 1.2), (long)(SlideBuilder.EmuCm * 1.2),
                num, fontSize: 1200, fontColor: "FFFFFF", bold: true,
                align: D.TextAlignmentTypeValues.Center,
                anchor: D.TextAnchoringTypeValues.Center));
            tree.Append(SlideBuilder.CreateTextBox(
                (long)(SlideBuilder.EmuCm * 3.8), boxY, SlideBuilder.EmuCm * 8, (long)(SlideBuilder.EmuCm * 1.2),
                title, fontSize: 1400, fontColor: "333333", bold: true,
                anchor: D.TextAnchoringTypeValues.Center));
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 13, boxY, SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 1.2),
                desc, fontSize: 1300, fontColor: "666666",
                anchor: D.TextAnchoringTypeValues.Center));
        }

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 17,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "설정 파일과 로그는 유지됩니다 (덮어쓰기 없음).",
            fontSize: 1400, fontColor: "16A34A", bold: true));
    }

    public static void AddConfigSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "설정 파일 구조", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // config.json
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 12,
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "config.json", fontSize: 1600, fontColor: AccentColor, bold: true,
            fontFamily: "Consolas"));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 9,
            new[]
            {
                "Erweka 설정",
                "  프로세스 이름, 구분 문자열",
                "  TCP 포트, 체크 주기",
                "TabmachineIF 설정",
                "  프로세스 이름, 실행 경로",
                "  재시작 횟수, 쿨다운",
                "PdfFolder 설정",
                "  경로, 유휴/백로그 임계치",
                "DbConnectionVerified 플래그"
            },
            fontSize: 1300, fontColor: "374151"));

        // dbconfig.json
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 12,
            "F5F6FA"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "dbconfig.json", fontSize: 1600, fontColor: AccentColor, bold: true,
            fontFamily: "Consolas"));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 9,
            new[]
            {
                "Server (DB 서버 주소)",
                "Database (DB 이름)",
                "UserId (로그인 사용자)",
                "Password (로그인 비밀번호)",
                "PlantCode (공장 코드)",
                "",
                "SYS_ALARM 테이블에",
                "알람 INSERT 시 사용"
            },
            fontSize: 1300, fontColor: "374151"));

        // Bottom note
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 16),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "서비스 핫리로드: 설정 파일 변경 시 FileSystemWatcher가 자동 감지 → 서비스 재시작 불필요",
            fontSize: 1400, fontColor: "16A34A", bold: true));
    }

    public static void AddAdvancedTroubleshootSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "고급 문제 해결", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30,
            new[] { "증상", "원인", "해결 방법" },
            new[]
            {
                new[] { "DB 알람 미기록", "dbconfig.json 미설정\n또는 연결 실패", "dbconfig.json 확인\n설정 → 연결 테스트" },
                new[] { "자동 재시작 안 됨", "실행 파일 경로 미지정", "config.json →\nTabmachineIF.ExecutablePath 확인" },
                new[] { "서비스 시작 후 미동작", "config.json 미생성\n(최초 설치)", "감시 프로그램 실행 →\n초기 설정 완료" },
                new[] { "서비스 등록 실패", "관리자 권한 부족", "관리자 권한으로 실행\n(UAC 승인)" },
                new[] { "포트 감시 오탐", "방화벽 또는\n네트워크 이슈", "Port 값 확인\n방화벽 규칙 점검" }
            },
            rowHeight: 600000, fontSize: 1300);
        tree.Append(table);

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 14,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "서비스 상태 확인:  sc query InterfaceWatchDog",
            fontSize: 1400, fontColor: "333333", bold: true,
            fontFamily: "Consolas"));
    }
}
