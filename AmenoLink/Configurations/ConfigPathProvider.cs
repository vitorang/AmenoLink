using AmenoLink.Shared;
using System.Text.Json;

namespace AmenoLink.Configurations;

internal static class ConfigPathProvider
{
#if DEBUG
    private const string FolderName = "AmenoLink-Debug";
#else
    private const string FolderName = "AmenoLink";
#endif

    private static readonly string BaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        FolderName
    );

    public static string GetConfigDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
        return BaseDirectory;
    }

    private static T[] LoadConfigs<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            T[] defaultConfig = [];
            SaveConfigs(filePath, defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T[]>(json, JsonDefaults.Options) ?? [];
    }

    private static void SaveConfigs<T>(string filePath, T[] configs)
    {
        string json = JsonSerializer.Serialize(configs, JsonDefaults.Options);
        File.WriteAllText(filePath, json);
    }

    private static T LoadSingleConfig<T>(string filePath, Func<T> createDefault)
    {
        if (!File.Exists(filePath))
        {
            T defaultConfig = createDefault();
            SaveSingleConfig(filePath, defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonDefaults.Options) ?? createDefault();
    }

    private static void SaveSingleConfig<T>(string filePath, T config)
    {
        string json = JsonSerializer.Serialize(config, JsonDefaults.Options);
        File.WriteAllText(filePath, json);
    }

    public static class Program
    {
        private const string ConfigFileName = "program-config.json";

        public static string GetFilePath() => Path.Combine(GetConfigDirectory(), ConfigFileName);

        public static ProgramConfig[] LoadConfigs() => ConfigPathProvider.LoadConfigs<ProgramConfig>(GetFilePath());

        public static void SaveConfigs(ProgramConfig[] configs) => ConfigPathProvider.SaveConfigs(GetFilePath(), configs);
    }

    public static class Cache
    {
        private const string ConfigFileName = "cache-config.json";

        public static string GetFilePath() => Path.Combine(GetConfigDirectory(), ConfigFileName);

        public static CacheConfig[] LoadConfigs() => ConfigPathProvider.LoadConfigs<CacheConfig>(GetFilePath());

        public static void SaveConfigs(CacheConfig[] configs) => ConfigPathProvider.SaveConfigs(GetFilePath(), configs);
    }

    public static class General
    {
        private const string ConfigFileName = "general-config.json";

        public static string GetFilePath() => Path.Combine(GetConfigDirectory(), ConfigFileName);

        public static GeneralConfig LoadConfig() => ConfigPathProvider.LoadSingleConfig(GetFilePath(), () => new GeneralConfig());

        public static void SaveConfig(GeneralConfig config) => ConfigPathProvider.SaveSingleConfig(GetFilePath(), config);
    }

    public static class Topic
    {
        private const string ConfigFileName = "topic-config.json";

        public static string GetFilePath() => Path.Combine(GetConfigDirectory(), ConfigFileName);

        public static TopicConfig[] LoadConfigs() => ConfigPathProvider.LoadConfigs<TopicConfig>(GetFilePath());

        public static void SaveConfigs(TopicConfig[] configs) => ConfigPathProvider.SaveConfigs(GetFilePath(), configs);
    }
}
