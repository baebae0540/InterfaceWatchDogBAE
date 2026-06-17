using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Tests.Models;

public class AppConfigTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _tempExe;

    public AppConfigTests()
    {
        Directory.CreateDirectory(_tempDir);
        _tempExe = Path.Combine(_tempDir, "fake.exe");
        File.WriteAllText(_tempExe, "");
    }

    // ── TabmachineConfig.CanRestart ─────────────────────────────────────────

    [Fact]
    public void CanRestart_WhenFileExists_ShouldBeTrue()
    {
        var cfg = new TabmachineConfig { ExecutablePath = _tempExe };
        cfg.CanRestart.Should().BeTrue();
    }

    [Fact]
    public void CanRestart_WhenFileNotExists_ShouldBeFalse()
    {
        var cfg = new TabmachineConfig { ExecutablePath = Path.Combine(_tempDir, "notexist.exe") };
        cfg.CanRestart.Should().BeFalse();
    }

    [Fact]
    public void CanRestart_WhenPathEmpty_ShouldBeFalse()
    {
        var cfg = new TabmachineConfig { ExecutablePath = "" };
        cfg.CanRestart.Should().BeFalse();
    }

    // ── PdfFolderConfig.IsConfigured ─────────────────────────────────────────

    [Fact]
    public void IsConfigured_WhenDirectoryExists_ShouldBeTrue()
    {
        var cfg = new PdfFolderConfig { Path = _tempDir };
        cfg.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WhenDirectoryNotExists_ShouldBeFalse()
    {
        var cfg = new PdfFolderConfig { Path = Path.Combine(_tempDir, "nosuchfolder") };
        cfg.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenPathEmpty_ShouldBeFalse()
    {
        var cfg = new PdfFolderConfig { Path = "" };
        cfg.IsConfigured.Should().BeFalse();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
