namespace AmenoLink.Dtos;

public record ActionResponse(
    bool Success,
    string[]? Logs = null,
    object? Result = null,
    ActionError? Error = null,
    string Id = "",
    Message? Previous = null,
    string Type = "ActionResponse",
    DateTimeOffset CreatedAt = default,
    string AppName = ""
) : Message(Id, Previous, Type, CreatedAt, AppName)
{
    public string[] Logs { get; init; } = Logs ?? [];
}
