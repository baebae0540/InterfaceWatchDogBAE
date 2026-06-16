using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Tests.Integration;

// 실제 ConfigManager가 사용하는 ProgramData 경로(C:\ProgramData\InterfaceWatchDog\config.json)에
// 직접 읽고 쓰는 통합 테스트. 실행 전후로 기존 설정 파일을 백업/복원하여 운영 설정을 보존한다.
// 별도 실행: dotnet test --filter Category=Integration
[Trait("Category", "Integration")]
public class ConfigManagerIntegrationTests : IDisposable
{
    private readonly bool   _hadExistingConfig;
    private readonly string _backupPath = ConfigManager.ConfigFilePath + ".bak_" + Guid.NewGuid();

    public ConfigManagerIntegrationTests()
    {
        _hadExistingConfig = File.Exists(ConfigManager.ConfigFilePath);
        if (_hadExistingConfig)
            File.Copy(ConfigManager.ConfigFilePath, _backupPath, overwrite: true);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesAllSettings()
    {
        var config = new AppConfig
        {
            Erweka = new ProgramConfig
            {
                DisplayName = "통합테스트ERWEKA", ProcessName = "erweka_proc",
                ExecutablePath = @"C:\test\erweka.exe", Arguments = "-x",
                MaxRestartAttempts = 5, RestartCooldownSeconds = 45, ProcessCheckSeconds = 10
            },
            TabmachineIF = new ProgramConfig
            {
                DisplayName = "통합테스트Tab", ProcessName = "tab_proc",
                ExecutablePath = @"C:\test\tab.exe",
                MaxRestartAttempts = 2, RestartCooldownSeconds = 20, ProcessCheckSeconds = 15
            },
            PdfFolder = new PdfFolderConfig { Path = @"C:\pdf", MaxIdleMinutes = 15, MaxBacklogCount = 20, FileActivityCheckMinutes = 3 }
        };

        ConfigManager.Save(config);
        var loaded = ConfigManager.Load();

        loaded.Should().BeEquivalentTo(config);
    }

    [Fact]
    public void IsFirstRun_AfterSave_ReturnsFalse()
    {
        ConfigManager.Save(new AppConfig());
        ConfigManager.IsFirstRun().Should().BeFalse();
    }

    [Fact]
    public void Load_WhenConfigFileMissing_ReturnsDefaultConfig()
    {
        if (File.Exists(ConfigManager.ConfigFilePath))
            File.Delete(ConfigManager.ConfigFilePath);

        ConfigManager.IsFirstRun().Should().BeTrue();

        var loaded = ConfigManager.Load();
        loaded.Erweka.ProcessName.Should().Be("javaw");
        loaded.TabmachineIF.ProcessName.Should().Be("TabmachineIF");
    }

    public void Dispose()
    {
        if (_hadExistingConfig)
        {
            File.Copy(_backupPath, ConfigManager.ConfigFilePath, overwrite: true);
            File.Delete(_backupPath);
        }
        else if (File.Exists(ConfigManager.ConfigFilePath))
        {
            File.Delete(ConfigManager.ConfigFilePath);
        }
    }
}
