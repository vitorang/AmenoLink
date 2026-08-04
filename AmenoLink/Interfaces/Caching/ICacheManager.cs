using System.Text.Json;

namespace AmenoLink.Interfaces.Caching;

internal interface ICacheManager
{
    void LoadConfigurations();
    JsonElement? Get(string groupName, string key);
    void Set(string groupName, string key, JsonElement value);
    void Delete(string groupName, string key);
    Dictionary<string, JsonElement?> All(string groupName);
    void Clear(string groupName);
}
