using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using PptGenerator;
using PptGenerator.Slides;

var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Docs");
Directory.CreateDirectory(outputDir);

// ── 1. 사용법 PPT (운영자 + IT관리자) ────────────────────────────────────
var usageFileName = "InterfaceWatchDog_사용법.pptx";
var usagePath = Path.Combine(outputDir, usageFileName);
if (!TryEnsureWritable(usagePath, usageFileName)) return 1;

Console.WriteLine("=== InterfaceWatchDog 사용법 PPT 생성 ===");

using (var doc = PresentationDocument.Create(usagePath, PresentationDocumentType.Presentation))
{
    var builder = new SlideBuilder(doc);

    // === 공통 섹션 (1~4) ===
    CommonSlides.AddCoverSlide(builder);         // 1. 표지
    CommonSlides.AddTocSlide(builder);           // 2. 목차
    CommonSlides.AddIntroSlide(builder);         // 3. 프로그램 소개
    CommonSlides.AddArchitectureSlide(builder);  // 4. 시스템 구성도

    // === 현장 운영자 섹션 (5~11) ===
    OperatorSlides.AddSectionTitle(builder);     // 5. 섹션 구분
    OperatorSlides.AddInstallSlide(builder);     // 6. 설치하기
    OperatorSlides.AddErwekaSetupSlide(builder); // 7. 초기 설정 ① ERWEKA
    OperatorSlides.AddTabSetupSlide(builder);    // 8. 초기 설정 ② TabmachineIF
    OperatorSlides.AddDashboardSlide(builder);   // 9. 상태 대시보드
    OperatorSlides.AddTrayIconSlide(builder);    // 10. 트레이 아이콘 & 알림
    OperatorSlides.AddTroubleshootSlide(builder);// 11. 현장 문제 대응

    // === IT 관리자 섹션 (12~18) ===
    AdminSlides.AddSectionTitle(builder);        // 12. 섹션 구분
    AdminSlides.AddServiceSlide(builder);        // 13. 서비스 관리
    AdminSlides.AddDbAlarmSlide(builder);        // 14. DB 알람 설정
    AdminSlides.AddLogSlide(builder);            // 15. 로그 관리
    AdminSlides.AddPatchSlide(builder);          // 16. 패치 배포
    AdminSlides.AddConfigSlide(builder);         // 17. 설정 파일 구조
    AdminSlides.AddAdvancedTroubleshootSlide(builder); // 18. 고급 문제 해결

    // === 공통 마무리 (19) ===
    CommonSlides.AddSummarySlide(builder);       // 19. 요약 & 참고
}

VerifyPpt(usagePath, "사용법", 19);

// ── 2. 개발자 인수인계 PPT ───────────────────────────────────────────────
var handoverFileName = "InterfaceWatchDog_인수인계.pptx";
var handoverPath = Path.Combine(outputDir, handoverFileName);
if (!TryEnsureWritable(handoverPath, handoverFileName)) return 1;

Console.WriteLine();
Console.WriteLine("=== InterfaceWatchDog 인수인계 PPT 생성 ===");

SlideBuilder.ResetShapeIdCounter();

using (var doc = PresentationDocument.Create(handoverPath, PresentationDocumentType.Presentation))
{
    var builder = new SlideBuilder(doc);

    // === 표지 + 개요 (1~2) ===
    HandoverSlides.AddCoverSlide(builder);            // 1. 표지
    HandoverSlides.AddSectionTitle(builder);           // 2. 섹션 소개

    // === 핵심 내용 (3~7) ===
    HandoverSlides.AddArchitectureSlide(builder);      // 3. 시스템 아키텍처
    HandoverSlides.AddCodeStructureSlide(builder);     // 4. 코드 구조
    HandoverSlides.AddCoreLogicSlide(builder);         // 5. 핵심 로직 흐름
    HandoverSlides.AddBuildDeploySlide(builder);       // 6. 빌드 및 배포
    HandoverSlides.AddMaintenanceSlide(builder);       // 7. 유지보수 포인트
}

VerifyPpt(handoverPath, "인수인계", 7);

Console.WriteLine();
Console.WriteLine("모든 PPT 생성 완료.");
return 0;

// ── 헬퍼 ─────────────────────────────────────────────────────────────────

static bool TryEnsureWritable(string path, string displayName)
{
    if (!File.Exists(path)) return true;
    try { using var _ = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return true; }
    catch (IOException)
    {
        Console.Error.WriteLine($"오류: {displayName} 파일이 다른 프로세스에서 사용 중입니다. 파일을 닫고 다시 실행하세요.");
        return false;
    }
}

static void VerifyPpt(string path, string label, int expectedCount)
{
    var fullPath = Path.GetFullPath(path);
    Console.WriteLine($"생성 완료: {fullPath}");

    Console.WriteLine($"=== {label} 검증 ===");
    using var verify = PresentationDocument.Open(fullPath, false);
    var slides = verify.PresentationPart!.Presentation.SlideIdList!
        .Elements<DocumentFormat.OpenXml.Presentation.SlideId>().ToList();
    Console.WriteLine($"슬라이드 수: {slides.Count} (예상: {expectedCount})");

    int idx = 0;
    foreach (var slideId in slides)
    {
        idx++;
        var slidePart = (SlidePart)verify.PresentationPart.GetPartById(slideId.RelationshipId!);
        var xml = slidePart.Slide.OuterXml;
        var textMatches = System.Text.RegularExpressions.Regex.Matches(xml, @"<a:t>([^<]*)</a:t>");
        var texts = textMatches.Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        Console.WriteLine($"  Slide {idx}: {texts.Count}개 텍스트 | " +
            (texts.Count > 0 ? string.Join(" / ", texts.Take(3)) + (texts.Count > 3 ? " ..." : "") : "(비어 있음)"));
    }
}
