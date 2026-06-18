using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;

namespace PptGenerator.Slides;

public static class OperatorSlides
{
    private const string AccentColor = "2563EB";

    public static void AddSectionTitle(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.SetSlideBackground(slide, AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 3,
            "현장 운영자 가이드",
            fontSize: 3600, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Center,
            align: D.TextAlignmentTypeValues.Center));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 4, SlideBuilder.EmuCm * 9,
            SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
            "설치부터 일상 모니터링까지",
            fontSize: 2000, fontColor: "BDD4FF",
            align: D.TextAlignmentTypeValues.Center));
    }

    public static void AddInstallSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "설치하기", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        var steps = new[]
        {
            ("Step 1", "설치 파일 실행", "InterfaceWatchDog_Setup_vX.X.X.exe 파일을\n더블클릭하여 실행합니다."),
            ("Step 2", "설치 유형 선택", "\"전체 설치\"를 선택합니다. (권장)\nWindows 서비스가 함께 등록됩니다."),
            ("Step 3", "설치 완료", "설치가 완료되면 초기 설정 화면이\n자동으로 표시됩니다.")
        };

        for (int i = 0; i < steps.Length; i++)
        {
            var (step, title, desc) = steps[i];
            var boxY = (long)(SlideBuilder.EmuCm * (3.5 + i * 4.5));

            // Step number circle
            tree.Append(SlideBuilder.CreateFilledRect(
                SlideBuilder.EmuCm * 2, boxY, SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 1.5), AccentColor));
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 2, boxY, SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 1.5),
                step, fontSize: 1400, fontColor: "FFFFFF", bold: true,
                align: D.TextAlignmentTypeValues.Center,
                anchor: D.TextAnchoringTypeValues.Center));

            // Title
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 6, boxY, SlideBuilder.EmuCm * 26, (long)(SlideBuilder.EmuCm * 1.5),
                title, fontSize: 1800, fontColor: "333333", bold: true,
                anchor: D.TextAnchoringTypeValues.Center));

            // Description
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 6, boxY + (long)(SlideBuilder.EmuCm * 1.8),
                SlideBuilder.EmuCm * 26, SlideBuilder.EmuCm * 2,
                desc, fontSize: 1400, fontColor: "666666"));
        }
    }

    public static void AddErwekaSetupSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "초기 설정 ① — ERWEKA Export Manager", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "설정 탭에서 ERWEKA Export Manager 감시 항목을 설정합니다.",
            fontSize: 1500, fontColor: "555555"));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30,
            new[] { "설정 항목", "설명", "기본값" },
            new[]
            {
                new[] { "프로세스 이름", "\"가져오기\" 버튼으로 실행 중인 프로그램에서 선택", "javaw" },
                new[] { "프로세스 구분 문자열", "동일 이름 프로세스 구별 (예: Export Manager)", "(비어 있음)" },
                new[] { "TCP 포트 감시", "포트 응답 감시 (0 = 비활성)", "0" },
                new[] { "프로세스 체크 주기", "감시 반복 간격 (10~300초)", "30초" }
            },
            rowHeight: 500000, fontSize: 1300);
        tree.Append(table);

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 13.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "TIP: \"가져오기\" 버튼을 클릭하면 현재 실행 중인 프로그램 목록에서 선택할 수 있습니다.",
            fontSize: 1400, fontColor: AccentColor, bold: true));
    }

    public static void AddTabSetupSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "초기 설정 ② — TabmachineIF", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "설정 탭에서 TabmachineIF 감시 및 재시작 항목을 설정합니다.",
            fontSize: 1500, fontColor: "555555"));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30,
            new[] { "설정 항목", "설명", "기본값" },
            new[]
            {
                new[] { "프로세스 이름", "\"가져오기\" 버튼으로 선택", "TabmachineIF" },
                new[] { "실행 파일 경로", "\"찾아보기\"로 exe 경로 지정 (재시작에 필수!)", "(비어 있음)" },
                new[] { "실행 인수", "재시작 시 전달할 명령행 인수 (선택)", "(비어 있음)" },
                new[] { "최대 재시작 횟수", "연속 재시작 제한 (1~10회)", "3회" },
                new[] { "프로세스 체크 주기", "감시 반복 간격 (10~300초)", "30초" }
            },
            rowHeight: 450000, fontSize: 1300);
        tree.Append(table);

        tree.Append(SlideBuilder.CreateFilledRect(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 13.5),
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 2.5),
            "FEF2F2"));
        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 3, (long)(SlideBuilder.EmuCm * 14),
            SlideBuilder.EmuCm * 28, SlideBuilder.EmuCm * 2,
            "주의: 실행 파일 경로를 지정하지 않으면 감시만 수행되고 자동 재시작이 동작하지 않습니다!",
            fontSize: 1400, fontColor: "DC2626", bold: true));
    }

    public static void AddDashboardSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "상태 대시보드 사용법", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "트레이 아이콘 더블클릭 또는 우클릭 → \"상태 대시보드\"로 열 수 있습니다.",
            fontSize: 1500, fontColor: "555555"));

        // Dashboard layout description
        var sections = new[]
        {
            ("상단 — 프로그램 상태 카드", "ERWEKA / TabmachineIF 각각의 상태를 색상과 텍스트로 표시\n알람 건수, 재시작 횟수, 마지막 감지 시간 확인 가능", "2563EB"),
            ("중단 — PDF 폴더 감시", "ERWEKA가 생성하는 PDF 파일의 활동을 감시\n유휴 경고, 백로그 경고 표시 (활성화 시에만 표시)", "16A34A"),
            ("하단 — 실시간 로그", "감시 엔진의 동작 로그를 실시간으로 스트리밍\nINFO / WARN / ERROR 레벨별 표시", "8B5CF6")
        };

        for (int i = 0; i < sections.Length; i++)
        {
            var (title, desc, color) = sections[i];
            var boxY = (long)(SlideBuilder.EmuCm * (5 + i * 4));

            tree.Append(SlideBuilder.CreateFilledRect(
                SlideBuilder.EmuCm * 2, boxY, (long)(SlideBuilder.EmuCm * 0.4), (long)(SlideBuilder.EmuCm * 3), color));
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 3, boxY, SlideBuilder.EmuCm * 29, SlideBuilder.EmuCm * 1,
                title, fontSize: 1600, fontColor: color, bold: true));
            tree.Append(SlideBuilder.CreateTextBox(
                SlideBuilder.EmuCm * 3, boxY + (long)(SlideBuilder.EmuCm * 1.3),
                SlideBuilder.EmuCm * 29, SlideBuilder.EmuCm * 2,
                desc, fontSize: 1400, fontColor: "555555"));
        }
    }

    public static void AddTrayIconSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "트레이 아이콘 & 알림", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "시스템 트레이(작업 표시줄 우하단)의 아이콘 색상으로 현재 상태를 확인할 수 있습니다.",
            fontSize: 1500, fontColor: "555555"));

        // Status color table
        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30,
            new[] { "아이콘 색상", "상태", "의미", "조치" },
            new[]
            {
                new[] { "녹색", "정상", "모든 프로세스 정상 실행 중", "조치 불필요" },
                new[] { "주황색", "경고", "이상 징후 감지", "대시보드에서 상세 확인" },
                new[] { "파란색", "재시작 중", "프로세스 자동 재시작 시도 중", "잠시 대기" },
                new[] { "빨간색", "복구 실패", "재시작 실패 또는 프로세스 중지", "수동 확인 필요" },
                new[] { "회색", "비활성", "감시 비활성 또는 초기화 중", "잠시 대기" }
            },
            rowHeight: 420000, fontSize: 1300);
        tree.Append(table);

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 13,
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "우클릭 메뉴: 상태 대시보드 / 로그 보기 / 설정 / 종료",
            fontSize: 1500, fontColor: "333333", bold: true));

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, (long)(SlideBuilder.EmuCm * 14.5),
            SlideBuilder.EmuCm * 30, SlideBuilder.EmuCm * 2,
            "장애 발생 시 풍선 알림이 자동으로 표시됩니다 (5초간).",
            fontSize: 1400, fontColor: "DC2626"));
    }

    public static void AddTroubleshootSlide(SlideBuilder builder)
    {
        var slide = builder.AddSlide();
        SlideBuilder.AddHeaderBar(slide, "현장 문제 대응", AccentColor);

        var tree = slide.Slide.CommonSlideData!.ShapeTree!;

        tree.Append(SlideBuilder.CreateTextBox(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 3,
            SlideBuilder.EmuCm * 30, (long)(SlideBuilder.EmuCm * 1.5),
            "자주 발생하는 상황과 대응 방법입니다.",
            fontSize: 1500, fontColor: "555555"));

        var table = SlideBuilder.CreateTableFrame(
            SlideBuilder.EmuCm * 2, SlideBuilder.EmuCm * 5,
            SlideBuilder.EmuCm * 30,
            new[] { "증상", "확인 방법", "대응" },
            new[]
            {
                new[] { "트레이 아이콘 빨간색", "대시보드에서 상태 확인", "대상 프로그램 수동 실행\nIT 담당자에게 연락" },
                new[] { "풍선 알림 \"복구 실패\"", "재시작 횟수 초과 확인", "프로그램 수동 확인\n로그 뷰어에서 이력 확인" },
                new[] { "아이콘 주황색", "경고 상태 확인", "대시보드에서 상세 메시지 확인" },
                new[] { "아이콘 회색 (지속)", "감시 비활성 확인", "잠시 대기\n지속 시 IT 담당자에게 연락" }
            },
            rowHeight: 550000, fontSize: 1300);
        tree.Append(table);
    }
}
