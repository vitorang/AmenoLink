using AmenoLink.Dtos.Configuration;
using AmenoLink.Interfaces.Managers.Configuration;

namespace AmenoLink.Managers.Configuration;

internal class ProjectManager : IProjectManager
{
    private const string PythonFilter = "Requisitos do Python|requirements.txt|Arquivos de Texto (*.txt)|*.txt";
    private const string PythonTitle = "Selecionar Manifesto do Python (requirements.txt)";

    private const string DartFilter = "Especificação do Pacote Dart|pubspec.yaml|Arquivos YAML (*.yaml;*.yml)|*.yaml;*.yml";
    private const string DartTitle = "Selecionar Manifesto do Dart (pubspec.yaml)";

    private const string ManifestFileNotFoundError = "Arquivo de manifesto não foi encontrado";
    private const string UnknownProjectTypeError = "Tipo de projeto desconhecido";
    private const string PackageNotFoundError = "Biblioteca não encontrada";

    public (string Filter, string Title)? GetManifestDialogOptions(string type)
    {
        if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
            return (PythonFilter, PythonTitle);

        if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
            return (DartFilter, DartTitle);

        return null;
    }

    public PackageVersion GetPackageVersion(string manifestPath, string type)
    {
        string appVersion = typeof(ProjectManager).Assembly.GetName().Version?.ToString(3) ?? "0.0.1";

        try
        {
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
                return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: ManifestFileNotFoundError);

            if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
                return GetPythonPackageVersion(manifestPath, appVersion);

            if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
                return GetDartPackageVersion(manifestPath, appVersion);

            return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: UnknownProjectTypeError);
        }
        catch (Exception exception)
        {
            return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: exception.Message);
        }
    }

    private static PackageVersion GetPythonPackageVersion(string manifestPath, string appVersion)
    {
        var lines = File.ReadAllLines(manifestPath);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("amenolink", StringComparison.OrdinalIgnoreCase))
            {
                string remainder = line["amenolink".Length..].Trim();

                if (string.IsNullOrEmpty(remainder))
                    return new PackageVersion(ManifestPath: manifestPath, Version: "*", AppVersion: appVersion);

                char[] versionPrefixes = ['=', '>', '<', '~', '!'];
                string version = remainder.TrimStart(versionPrefixes).Trim();

                if (string.IsNullOrEmpty(version))
                    return new PackageVersion(ManifestPath: manifestPath, Version: "*", AppVersion: appVersion);

                bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
                return new PackageVersion(manifestPath, version, appVersion, isCompatible);
            }
        }

        return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: PackageNotFoundError);
    }

    private static PackageVersion GetDartPackageVersion(string manifestPath, string appVersion)
    {
        var lines = File.ReadAllLines(manifestPath);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("amenolink:", StringComparison.OrdinalIgnoreCase))
            {
                string version = line["amenolink:".Length..].Trim().Trim('^', '"', '\'').Trim();

                if (string.IsNullOrEmpty(version))
                    return new PackageVersion(ManifestPath: manifestPath, Version: "*", AppVersion: appVersion);

                bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
                return new PackageVersion(manifestPath, version, appVersion, isCompatible);
            }
        }

        return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: PackageNotFoundError);
    }
}
