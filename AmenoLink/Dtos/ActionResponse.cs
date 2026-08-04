namespace AmenoLink.Dtos;

public record ActionResponse(
    bool Success,
    string[]? Logs = null,
    object? Response = null,
    string? ErrorType = null,
    string? ErrorMessage = null,
    string Id = "",
    Message? Previous = null,
    string Type = "ActionResponse",
    DateTimeOffset CreatedAt = default
) : Message(Id, Previous, Type, CreatedAt)
{
    public string[] Logs { get; init; } = Logs ?? [];
}
