namespace AmenoLink.Dtos;

public record ActionResponse(
    ActionRequest ActionRequest,
    bool Success,
    string[]? Logs = null,
    string Id = "",
    string? Response = null,
    string? ErrorType = null,
    string? ErrorMessage = null
)
{
    public string Id { get; init; } = string.IsNullOrEmpty(Id) ? Ulid.NewUlid().ToString() : Id;
    public string[] Logs { get; init; } = Logs ?? [];
}
