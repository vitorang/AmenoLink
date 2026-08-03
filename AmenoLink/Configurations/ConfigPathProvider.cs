using AmenoLink.Shared;
using System.Text.Json;

namespace AmenoLink.Configurations;

internal static class ConfigPathProvider
{
    private const string ProgramConfigFileName = "program-config.json";
    private const string CacheConfigFileName = "cache-config.json";

    private static readonly string BaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AmenoLink"
    );

    public static string GetConfigDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
        return BaseDirectory;
    }

    public static string GetProgramConfigFilePath()
    {
        return Path.Combine(GetConfigDirectory(), ProgramConfigFileName);
    }

    public static string GetCacheConfigFilePath()
    {
        return Path.Combine(GetConfigDirectory(), CacheConfigFileName);
    }

    public static ProgramConfig[] LoadProgramConfigs()
    {
        string filePath = GetProgramConfigFilePath();

        if (!File.Exists(filePath))
        {
            ProgramConfig[] defaultConfig = [];
            SaveProgramConfigs(defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ProgramConfig[]>(json, JsonDefaults.Options) ?? [];
    }

    public static void SaveProgramConfigs(ProgramConfig[] configs)
    {
        string filePath = GetProgramConfigFilePath();
        string json = JsonSerializer.Serialize(configs, JsonDefaults.Options);
        File.WriteAllText(filePath, json);
    }

    public static CacheConfig[] LoadCacheConfigs()
    {
        string filePath = GetCacheConfigFilePath();

        if (!File.Exists(filePath))
        {
            CacheConfig[] defaultConfig = [];
            SaveCacheConfigs(defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<CacheConfig[]>(json, JsonDefaults.Options) ?? [];
    }

    public static void SaveCacheConfigs(CacheConfig[] configs)
    {
        string filePath = GetCacheConfigFilePath();
        string json = JsonSerializer.Serialize(configs, JsonDefaults.Options);
        File.WriteAllText(filePath, json);
    }
}
