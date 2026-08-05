namespace AmenoLink.Hubs;

public sealed class ConnectedClient
{
    public required string ConnectionId { get; init; }
    public HashSet<string> Stores { get; } = [];
}
