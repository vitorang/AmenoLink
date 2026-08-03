using System.Text.Json;

namespace AmenoLink.Interfaces.Caching;

internal interface ICacheManager
{
    void LoadConfigurations();
    JsonElement? Get(string groupKey, string key);
    void Set(string groupKey, string key, JsonElement value);
    void Delete(string groupKey, string key);
    Dictionary<string, JsonElement?> All(string groupKey);
    void Clear(string groupKey);
}
