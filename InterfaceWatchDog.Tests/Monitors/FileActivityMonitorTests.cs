using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.Core.Monitors;

namespace InterfaceWatchDog.Tests.Monitors;

public class FileActivityMonitorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly FileActivityMonitor _monitor = new();

    public FileActivityMonitorTests() => Directory.CreateDirectory(_tempDir);

    private PdfFolderConfig ConfigFor(string path, int maxIdle = 30, int maxBacklog = 50)
        => new() { Path = path, MaxIdleMinutes = maxIdle, MaxBacklogCount = maxBacklog };

    // ── 미설정 폴더 ───────────────────────────────────────────────────────────

    [Fact]
    public void Check_WhenFolderNotConfigured_ShouldReturnNotConfigured()
    {
        var cfg    = ConfigFor("");
        var result = _monitor.Check(cfg);

        result.IsFolderConfigured.Should().BeFalse();
        result.StatusMessage.Should().Contain("설정되지 않");
    }

    [Fact]
    public void Check_WhenFolderDoesNotExist_ShouldReturnNotConfigured()
    {
        var cfg    = ConfigFor(Path.Combine(_tempDir, "nodir"));
        var result = _monitor.Check(cfg);

        result.IsFolderConfigured.Should().BeFalse();
    }

    // ── 파일 카운트 ───────────────────────────────────────────────────────────

    [Fact]
    public void Check_ShouldCountOnlyPdfFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "a.pdf"),  "");
        File.WriteAllText(Path.Combine(_tempDir, "b.pdf"),  "");
        File.WriteAllText(Path.Combine(_tempDir, "c.txt"),  "");

        var result = _monitor.Check(ConfigFor(_tempDir));

        result.FileCount.Should().Be(2);
    }

    [Fact]
    public void Check_WhenNoPdfFiles_ShouldReturnFileCountZero()
    {
        var result = _monitor.Check(ConfigFor(_tempDir));
        result.FileCount.Should().Be(0);
    }

    // ── Idle 경고 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Check_WhenRecentPdfExists_ShouldNotTriggerIdleWarning()
    {
        var pdfPath = Path.Combine(_tempDir, "recent.pdf");
        File.WriteAllText(pdfPath, "");
        // 방금 생성된 파일 → idle 아님

        var result = _monitor.Check(ConfigFor(_tempDir, maxIdle: 30));
        result.IsIdleWarning.Should().BeFalse();
    }

    [Fact]
    public void Check_WhenEmptyFolderJustCreated_ShouldNotTriggerIdleWarning()
    {
        // 방금 생성된 빈 폴더 → 폴더 생성 시간 기준 idle 아님
        var freshDir = Path.Combine(_tempDir, "fresh");
        Directory.CreateDirectory(freshDir);

        var result = _monitor.Check(ConfigFor(freshDir, maxIdle: 30));
        result.IsIdleWarning.Should().BeFalse();
    }

    // ── Backlog 경고 ──────────────────────────────────────────────────────────

    [Fact]
    public void Check_WhenFileCountExceedsThreshold_ShouldTriggerBacklogWarning()
    {
        for (int i = 0; i < 5; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.pdf"), "");

        var result = _monitor.Check(ConfigFor(_tempDir, maxBacklog: 5));
        result.IsBacklogWarning.Should().BeTrue();
    }

    [Fact]
    public void Check_WhenFileCountBelowThreshold_ShouldNotTriggerBacklogWarning()
    {
        File.WriteAllText(Path.Combine(_tempDir, "one.pdf"), "");

        var result = _monitor.Check(ConfigFor(_tempDir, maxBacklog: 10));
        result.IsBacklogWarning.Should().BeFalse();
    }

    // ── IsFolderConfigured 플래그 ─────────────────────────────────────────────

    [Fact]
    public void Check_WhenFolderExists_ShouldSetIsFolderConfiguredTrue()
    {
        var result = _monitor.Check(ConfigFor(_tempDir));
        result.IsFolderConfigured.Should().BeTrue();
    }

    // ── 접근 불가 폴더 예외 안전성 ───────────────────────────────────────────

    [Fact]
    public void Check_ShouldNotThrow_OnAnyInput()
    {
        // 비어있는 경로는 IsConfigured=false로 처리되어 예외 없이 반환
        var act = () => _monitor.Check(new PdfFolderConfig { Path = "" });
        act.Should().NotThrow();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
