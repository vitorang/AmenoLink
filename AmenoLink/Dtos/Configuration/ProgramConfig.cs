namespace AmenoLink.Dtos.Configuration;

public record ProgramConfig(
    string Id,
    string Path,
    ProgramConfig.Action[] Actions,
    int SlidingExpirationInSeconds,
    int StartupTimeoutInSeconds,
    int MaxInstances
)
{
    public record Action(
        string Route,
        int TimeoutInSeconds
    );
}
