namespace AmenoLink.Dtos.Configuration;

public record GeneralConfig(
    bool StartMinimizedToTray = false,
    bool MinimizeToTrayOnClose = true,
    int MaxMessageDepth = 5,
    int MaxTopicHistorySize = 20,
    ProjectConfig[]? Projects = null
)
{
    public ProjectConfig[] Projects { get; init; } = Projects ?? [];
}