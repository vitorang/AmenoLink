namespace AmenoLink.Dtos.Configuration;

public record PackageInstallInstructions(
    string CSharp = "",
    string Dart = "",
    string Python = "",
    string TypeScript = "",
    bool IsDebugging = false
);
