namespace AmenoLink.Dtos.Configuration;

public record PackageVersion(
    string ManifestPath = "",
    string Version = "",
    string AppVersion = "",
    bool IsCompatible = false,
    string? ErrorReason = null
);
