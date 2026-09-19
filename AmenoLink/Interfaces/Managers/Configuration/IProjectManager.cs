using AmenoLink.Dtos.Configuration;

namespace AmenoLink.Interfaces.Managers.Configuration;

internal interface IProjectManager
{
    (string Filter, string Title)? GetManifestDialogOptions(string type);
    PackageVersion GetPackageVersion(string manifestPath, string type);
}
