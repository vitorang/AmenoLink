using AmenoLink.Configurations;
using AmenoLink.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AmenoLink.Caching;

internal class CacheManager : ICacheManager
{
    private readonly ConcurrentDictionary<string, CacheGroupEntry> CacheGroups = new();

    private class CacheGroupEntry(CacheConfig config)
    {
        public MemoryCache Cache { get; set; } = new MemoryCache(new MemoryCacheOptions());
        public CacheConfig Config { get; set; } = config;
        public ConcurrentDictionary<string, byte> Keys { get; } = new();
    }

    public void LoadConfigurations()
    {
        var configs = ConfigPathProvider.Cache.LoadConfigs();
        var newConfigMap = configs.ToDictionary(c => c.GroupName);

        var existingKeys = CacheGroups.Keys.ToList();
        foreach (var groupName in existingKeys)
        {
            if (!newConfigMap.ContainsKey(groupName))
            {
                if (CacheGroups.TryRemove(groupName, out var removedEntry))
                    removedEntry.Cache.Dispose();
            }
        }

        foreach (var config in configs)
        {
            if (CacheGroups.TryGetValue(config.GroupName, out var entry))
                entry.Config = config;
            else
                CacheGroups[config.GroupName] = new CacheGroupEntry(config);
        }
    }

    public JsonElement? Get(string groupName, string key)
    {
        var entry = GetGroupEntry(groupName);
        if (entry.Cache.TryGetValue(key, out JsonElement value))
            return value;

        entry.Keys.TryRemove(key, out _);
        return null;
    }

    public void Set(string groupName, string key, JsonElement value)
    {
        var entry = GetGroupEntry(groupName);
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

    public void Delete(string groupName, string key)
    {
        var entry = GetGroupEntry(groupName);
        entry.Cache.Remove(key);
        entry.Keys.TryRemove(key, out _);
    }

    public Dictionary<string, JsonElement?> All(string groupName)
    {
        var entry = GetGroupEntry(groupName);
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

    public void Clear(string groupName)
    {
        var entry = GetGroupEntry(groupName);
        entry.Cache.Dispose();
        entry.Cache = new MemoryCache(new MemoryCacheOptions());
        entry.Keys.Clear();
    }

    private CacheGroupEntry GetGroupEntry(string groupName)
    {
        if (!CacheGroups.TryGetValue(groupName, out var entry))
            throw new KeyNotFoundException($"O grupo de cache '{groupName}' não foi encontrado.");

        return entry;
    }
}
