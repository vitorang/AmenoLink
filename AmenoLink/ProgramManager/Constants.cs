namespace AmenoLink.ProgramManager;

internal static class Constants
{
    public const string StartupTimeout = "AmenoLink.StartupTimeout";
    public const string StartupFailed = "AmenoLink.StartupFailed";
    public const string ActionTimeout = "AmenoLink.ActionTimeout";
    public const string ActionFailed = "AmenoLink.ActionFailed";
    public const string ActionNotFound = "AmenoLink.ActionNotFound";
    public const string ActionInvalidResponse = "AmenoLink.ActionInvalidResponse";

    public const string OnStartupSuccess = "[AmenoLink.StartupSuccess]";
    public const string OnStartupError = "[AmenoLink.StartupError]";
    public const string OnActionSuccess = "[AmenoLink.ActionSuccess]";
    public const string OnActionError = "[AmenoLink.ActionError]";
    public const string OnActionLogged = "[AmenoLink.ActionLog]";

    public const string ExeExtension = ".exe";
    public const string PyExtension = ".py";
    public static readonly string[] SupportedExtensions = [ExeExtension, PyExtension];
}
