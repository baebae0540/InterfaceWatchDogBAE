using System.Text.Json;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core;

public static class ConfigManager
{
    private static readonly string ConfigDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "InterfaceWatchDog");

    public static readonly string ConfigFilePath =
        Path.Combine(ConfigDirectory, "config.json");

    public static readonly string DbConfigFilePath =
        Path.Combine(ConfigDirectory, "dbconfig.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }

    public static AlarmDbConfig LoadAlarmDb()
    {
        if (!File.Exists(DbConfigFilePath))
            return new AlarmDbConfig();

        try
        {
            var json = File.ReadAllText(DbConfigFilePath);
            return JsonSerializer.Deserialize<AlarmDbConfig>(json, JsonOptions) ?? new AlarmDbConfig();
        }
        catch
        {
            return new AlarmDbConfig();
        }
    }

    public static bool IsFirstRun() => !File.Exists(ConfigFilePath);
}
