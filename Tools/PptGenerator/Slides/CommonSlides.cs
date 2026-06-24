using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;

namespace PptGenerator.Slides;

public static class CommonSlides
{
    public static void AddCoverSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.SetSlideBackground(slide, "1C2028");

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Accent line
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 6, SlideBuilder.EmuCm / 6,
            "2563EB"));

        // Title
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 6,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 4,
            "InterfaceWatchDog",
            fontSize: 4000, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Top));

        // Subtitle
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "기본 사용법",
            fontSize: 3200, fontColor: "8C94A5",
            anchor: D.TextAnchoringTypeValues.Top));

        // Description
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 12,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "ERWEKA / TabmachineIF 인터페이스 감시 시스템  |  v1.4.0",
            fontSize: 1600, fontColor: "6B7280",
            anchor: D.TextAnchoringTypeValues.Top));
    }

    public static void AddTocSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "목차");

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Operator section box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 14,
            "EBF5FF"));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.5),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "현장 운영자", fontSize: 2000, fontColor: "2563EB", bold: true));

        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 5.5),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 10,
            new[]
            {
                "프로그램 설치",
                "초기 설정 (ERWEKA / TabmachineIF)",
                "상태 대시보드 사용법",
                "트레이 아이콘 & 알림 이해",
                "현장 문제 대응 가이드"
            },
            fontSize: 1600, fontColor: "1E40AF"));

        // Admin section box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 14,
            "E8E9EE"));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.5),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "IT 관리자", fontSize: 2000, fontColor: "1C2028", bold: true));

        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 5.5),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 10,
            new[]
            {
                "Windows 서비스 관리",
                "DB 알람 설정 (SYS_ALARM)",
                "로그 관리 및 조회",
                "패치 배포 방법",
                "설정 파일 구조",
                "고급 문제 해결"
            },
            fontSize: 1600, fontColor: "374151"));
    }

    public static void AddIntroSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "프로그램 소개");

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "ERWEKA Export Manager와 TabmachineIF 프로세스를 실시간으로 감시하고,\n장애 발생 시 자동 재시작 및 알람을 제공하는 Windows 모니터링 프로그램",
            fontSize: 1600, fontColor: "555555"));

        // 3 feature boxes
        var features = new[]
        {
            ("프로세스 감시", "ERWEKA: 프로세스 + TCP 포트\nTabmachineIF: 프로세스 상태\n설정 가능한 감시 주기", "2563EB"),
            ("자동 재시작", "TabmachineIF 종료 감지 시 자동 재시작\n최대 재시작 횟수 제한\n쿨다운 대기 후 재시도", "16A34A"),
            ("알람 기록", "SYS_ALARM DB 테이블 자동 기록\nERWEKA 중지, Tab 재시작 실패 시\n중복 방지 + 복구 후 재알람", "DC2626")
        };

        for (int i = 0; i < features.Length; i++)
        {
            var (title, desc, color) = features[i];
            var boxX = SlideBuilder.EmuCm * 2 + (long)(SlideBuilder.EmuCm * 10.5 * i);

            tree.Append(SlideBuilder.CreateFilledRect(boxX, SlideBuilder.EmuCm * 6, (long)(SlideBuilder.EmuCm * 9.5), SlideBuilder.EmuCm / 4, color));
            tree.Append(SlideBuilder.CreateTextBox(
                boxX, (long)(SlideBuilder.EmuCm * 6.8),
                (long)(SlideBuilder.EmuCm * 9.5), SlideBuilder.EmuCm * 1,
                title, fontSize: 1800, fontColor: color, bold: true));
            tree.Append(SlideBuilder.CreateTextBox(
                boxX, (long)(SlideBuilder.EmuCm * 8.5),
                (long)(SlideBuilder.EmuCm * 9.5), SlideBuilder.EmuCm * 6,
                desc, fontSize: 1400, fontColor: "555555"));
        }
    }

    public static void AddArchitectureSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "시스템 구성도");

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Tray App box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 7,
            "EBF5FF"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "감시 프로그램 (대화형)", fontSize: 1800, fontColor: "2563EB", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 5,
            new[]
            {
                "사용자 세션에서 실행",
                "상태 대시보드 UI 제공",
                "프로세스 재시작 수행",
                "트레이 아이콘 알림",
                "설정 변경 UI"
            },
            fontSize: 1400, fontColor: "1E40AF"));

        // Service box
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 7,
            "E8E9EE"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "Windows 서비스 (백그라운드)", fontSize: 1800, fontColor: "1C2028", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 5,
            new[]
            {
                "Session 0에서 실행",
                "부팅 시 자동 시작",
                "로그인 없이 감시 가능",
                "설정 변경 자동 반영",
                "재시작 불가 (Session 0 제약)"
            },
            fontSize: 1400, fontColor: "374151"));

        // Role table
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 11,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "권장: 두 모드를 함께 사용 — 서비스가 부팅 시 감시, 감시 프로그램이 재시작 및 UI 담당",
            fontSize: 1500, fontColor: "DC2626", bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 13,
            SlideBuilder.EmuCm * 30,
            new[] { "역할", "감시 프로그램", "Windows 서비스" },
            new[]
            {
                new[] { "프로세스 감시", "O", "O" },
                new[] { "프로세스 재시작", "O", "X (Session 0)" },
                new[] { "UI / 알림", "O", "X" },
                new[] { "부팅 시 자동 실행", "X (로그인 필요)", "O" }
            },
            fontSize: 1300);
        tree.Append(table);
    }

    public static void AddSummarySlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "요약 & 참고");

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        // Operator summary
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "EBF5FF"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "현장 운영자 핵심 포인트", fontSize: 1600, fontColor: "2563EB", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 4.8),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "트레이 아이콘 색상으로 상태 확인 (녹색=정상)",
                "빨간색이면 대상 프로그램 수동 확인",
                "로그 뷰어로 이력 조회 가능"
            },
            fontSize: 1400, fontColor: "1E40AF"));

        // Admin summary
        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 18, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 14, SlideBuilder.EmuCm * 6,
            "E8E9EE"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 3.3),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 1,
            "IT 관리자 핵심 포인트", fontSize: 1600, fontColor: "1C2028", bold: true));
        tree.Append(SlideBuilder.CreateMultiLineBulletBox(
            SlideBuilder.EmuCm * 19, (long)(SlideBuilder.EmuCm * 4.8),
            SlideBuilder.EmuCm * 12, SlideBuilder.EmuCm * 4,
            new[]
            {
                "서비스 자동 시작 상태 확인 (sc query)",
                "패치 배포: apply_patch.ps1 실행",
                "DB 알람: dbconfig.json 연결 테스트 필수"
            },
            fontSize: 1400, fontColor: "374151"));

        // Path reference table
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 10.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 1,
            "주요 경로 참고", fontSize: 1600, fontColor: "333333", bold: true));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 12,
            SlideBuilder.EmuCm * 30,
            new[] { "항목", "경로" },
            new[]
            {
                new[] { "설정 파일", "%ProgramData%\\InterfaceWatchDog\\config.json" },
                new[] { "DB 설정", "%ProgramData%\\InterfaceWatchDog\\dbconfig.json" },
                new[] { "로그 파일", "%ProgramData%\\InterfaceWatchDog\\Logs\\watchdog_YYYY-MM-DD.log" },
                new[] { "설치 경로", "%ProgramFiles%\\InterfaceWatchDog\\" }
            },
            fontSize: 1300);
        tree.Append(table);
    }
}
