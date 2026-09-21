namespace AmenoLink.Dtos.Configuration;

public record PackageInstallInstructions(
    string Dart = "",
    string Python = "",
    string TypeScript = "",
    bool IsDebugging = false
);
