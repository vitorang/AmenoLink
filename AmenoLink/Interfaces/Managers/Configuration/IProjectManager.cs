using AmenoLink.Dtos.Configuration;

namespace AmenoLink.Interfaces.Managers.Configuration;

internal interface IProjectManager
{
    string? GetManifestFilter(string type);
    PackageVersion GetPackageVersion(string manifestPath, string type);
    PackageInstallInstructions GetInstallInstructions();
}
