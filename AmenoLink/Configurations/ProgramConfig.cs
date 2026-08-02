namespace AmenoLink.Configurations;

public record ProgramConfig(
    string Id,
    string Path,
    ProgramConfig.Handler[] Handlers,
    int SlidingExpirationInSeconds,
    int StartupTimeoutInSeconds,
    int MaxInstances
)
{
    public record Handler(
        string Route,
        int TimeoutInSeconds
    );
}
