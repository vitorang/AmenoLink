using AmenoLink.Configurations;
using AmenoLink.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AmenoLink.Caching;

internal class CacheManager : ICacheManager
{
    private readonly ConcurrentDictionary<string, CacheGroupEntry> CacheGroups = new();

    private class CacheGroupEntry
    {
        public MemoryCache Cache { get; set; }
        public CacheConfig Config { get; set; }
        public ConcurrentDictionary<string, byte> Keys { get; } = new();

        public CacheGroupEntry(CacheConfig config)
        {
            Config = config;
            Cache = new MemoryCache(new MemoryCacheOptions());
        }
    }

    public void LoadConfigurations()
    {
        var configs = ConfigPathProvider.LoadCacheConfigs();
        var newConfigMap = configs.ToDictionary(c => c.GroupKey);

        var existingKeys = CacheGroups.Keys.ToList();
        foreach (var groupKey in existingKeys)
        {
            if (!newConfigMap.ContainsKey(groupKey))
            {
                if (CacheGroups.TryRemove(groupKey, out var removedEntry))
                    removedEntry.Cache.Dispose();
            }
        }

        foreach (var config in configs)
        {
            if (CacheGroups.TryGetValue(config.GroupKey, out var entry))
                entry.Config = config;
            else
                CacheGroups[config.GroupKey] = new CacheGroupEntry(config);
        }
    }

    public JsonElement? Get(string groupKey, string key)
    {
        var entry = GetGroupEntry(groupKey);
        if (entry.Cache.TryGetValue(key, out JsonElement value))
            return value;

        entry.Keys.TryRemove(key, out _);
        return null;
    }

    public void Set(string groupKey, string key, JsonElement value)
    {
        var entry = GetGroupEntry(groupKey);
        var options = new MemoryCacheEntryOptions();

        if (entry.Config.InactivityExpirationInSeconds > 0)
            options.SetSlidingExpiration(TimeSpan.FromSeconds(entry.Config.InactivityExpirationInSeconds));

        if (entry.Config.TotalExpirationInSeconds > 0)
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(entry.Config.TotalExpirationInSeconds));

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string k)
                entry.Keys.TryRemove(k, out _);
        });

        entry.Cache.Set(key, value.Clone(), options);
        entry.Keys[key] = 0;
    }

    public void Delete(string groupKey, string key)
    {
        var entry = GetGroupEntry(groupKey);
        entry.Cache.Remove(key);
        entry.Keys.TryRemove(key, out _);
    }

    public Dictionary<string, JsonElement?> All(string groupKey)
    {
        var entry = GetGroupEntry(groupKey);
        var result = new Dictionary<string, JsonElement?>();

        foreach (var key in entry.Keys.Keys)
        {
            if (entry.Cache.TryGetValue(key, out JsonElement value))
                result[key] = value;
            else
                entry.Keys.TryRemove(key, out _);
        }

        return result;
    }

    public void Clear(string groupKey)
    {
        var entry = GetGroupEntry(groupKey);
        entry.Cache.Dispose();
        entry.Cache = new MemoryCache(new MemoryCacheOptions());
        entry.Keys.Clear();
    }

    private CacheGroupEntry GetGroupEntry(string groupKey)
    {
        if (!CacheGroups.TryGetValue(groupKey, out var entry))
            throw new KeyNotFoundException($"O grupo de cache '{groupKey}' não foi encontrado.");

        return entry;
    }
}
