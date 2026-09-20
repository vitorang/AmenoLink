namespace AmenoLink.Dtos.Configuration;

public record PackageInstallInstructions(
    string Dart = "",
    string Python = "",
    bool IsDebugging = false
);
