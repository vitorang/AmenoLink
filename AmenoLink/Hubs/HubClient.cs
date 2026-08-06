namespace AmenoLink.Hubs;

public sealed class HubClient
{
    public required string ConnectionId { get; init; }
    public required string AppName { get; init; }
    public HashSet<string> Topics { get; } = [];
}
