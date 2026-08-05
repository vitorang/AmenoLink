using AmenoLink.Dtos;

namespace AmenoLink.Hubs;

internal partial class MainHub
{
    private string TopicChannel(string name) => $"topic:{name}";
}
