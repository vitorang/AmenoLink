namespace AmenoLink.Configurations;

public record GeneralConfig(
    bool StartMinimizedToTray = false,
    bool MinimizeToTrayOnClose = true,
    int MaxMessageDepth = 5,
    int MaxTopicHistorySize = 20
);