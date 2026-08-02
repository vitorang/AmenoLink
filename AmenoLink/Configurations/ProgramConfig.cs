namespace AmenoLink.Configurations;

internal record ProgramConfig(
    string Path,
    ProgramConfig.Handler[] Handlers,
    int SlidingExpirationInSeconds,
    int StartupTimeoutInSeconds,
    int MaxInstances
)
{
    internal record Handler(
        string Route,
        int TimeoutInSeconds
    );
}
