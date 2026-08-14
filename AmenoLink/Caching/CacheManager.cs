using AmenoLink.Configurations;
using AmenoLink.Hubs;
using AmenoLink.Interfaces.Caching;
using AmenoLink.Interfaces.Hub;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AmenoLink.Caching;

internal class CacheManager(IHubService hubService) : ICacheManager
{
    private readonly ConcurrentDictionary<string, CacheGroupEntry> CacheGroups = new();

    private class CacheGroupEntry(CacheConfig config)
    {
        public MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());
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
                {
                    removedEntry.Cache.Dispose();
                    _ = hubService.RemoveCacheSubscribers(groupName);
                }
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

    public bool Exists(string groupName) => CacheGroups.ContainsKey(groupName);

    public HubClient[] ListSubscribers(string groupName) => hubService.ListCacheSubscribers(groupName);

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

        options.RegisterPostEvictionCallback((evictedKey, _, reason, _) =>
        {
            if (reason is EvictionReason.Replaced or EvictionReason.None)
                return;

            if (evictedKey is string k)
            {
                entry.Keys.TryRemove(k, out _);
                _ = hubService.PublishToCacheGroup(groupName, k, null);
            }
        });

        var clonedValue = value.Clone();
        entry.Cache.Set(key, clonedValue, options);
        entry.Keys[key] = 0;
        _ = hubService.PublishToCacheGroup(groupName, key, clonedValue);
    }

    public void Delete(string groupName, string key)
    {
        var entry = GetGroupEntry(groupName);
        entry.Cache.Remove(key);
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
        foreach (var key in entry.Keys.Keys)
            entry.Cache.Remove(key);
    }

    private CacheGroupEntry GetGroupEntry(string groupName)
    {
        if (!CacheGroups.TryGetValue(groupName, out var entry))
            throw new KeyNotFoundException($"O grupo de cache '{groupName}' não foi encontrado.");

        return entry;
    }
}
