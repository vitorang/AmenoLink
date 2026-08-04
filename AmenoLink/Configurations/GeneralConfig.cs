namespace AmenoLink.Configurations;

public record GeneralConfig(
    bool StartMinimizedToTray = false,
    int MaxMessageDepth = 5,
    int MaxTopicHistorySize = 20
);