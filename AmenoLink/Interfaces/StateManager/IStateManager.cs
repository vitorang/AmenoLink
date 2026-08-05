using System.Text.Json;

namespace AmenoLink.Interfaces.StateManager;

internal interface IStateManager
{
    void LoadConfigurations();
    void Set(string groupName, IReadOnlyDictionary<string, JsonElement> entries);
    void Delete(string groupName, IReadOnlyList<string> keys);
    Dictionary<string, JsonElement?> All(string groupName);
    void Clear(string groupName);
}
