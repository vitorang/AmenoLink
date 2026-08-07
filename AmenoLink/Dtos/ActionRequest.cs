namespace AmenoLink.Dtos;

public record ActionRequest(
    string Route,
    object? Payload,
    string Id = "",
    Message? Previous = null,
    string Type = "ActionRequest",
    DateTimeOffset CreatedAt = default,
    string AppName = ""
) : Message(Id, Previous, Type, CreatedAt, AppName);
