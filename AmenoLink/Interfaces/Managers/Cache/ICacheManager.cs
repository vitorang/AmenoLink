using AmenoLink.Hubs;
using System.Text.Json;

namespace AmenoLink.Interfaces.Managers.Cache;

internal interface ICacheManager
{
    void LoadConfigurations();
    bool Exists(string groupName);
    HubClient[] ListSubscribers(string groupName);
    JsonElement? Get(string groupName, string key);
    void Set(string groupName, string key, JsonElement value);
    void Delete(string groupName, string key);
    Dictionary<string, JsonElement?> All(string groupName);
    void Clear(string groupName);
}
