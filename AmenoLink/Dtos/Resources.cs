namespace AmenoLink.Dtos;

public record Resources(
    string[] Actions = null!,
    string[] Caches = null!,
    string[] Topics = null!,
    string Version = ""
)
{
    public string[] Actions { get; init; } = Actions ?? [];
    public string[] Caches { get; init; } = Caches ?? [];
    public string[] Topics { get; init; } = Topics ?? [];
}
