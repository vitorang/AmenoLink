using AmenoLink.Interfaces.Managers.Configuration;
using System.Text.RegularExpressions;

namespace AmenoLink.Managers.Configuration;

internal class ProjectManager : IProjectManager
{
    #region Common

    private const string ManifestFileNotFoundError = "Arquivo de manifesto não foi encontrado";
    private const string UnknownProjectTypeError = "Tipo de projeto desconhecido";
    private const string MissingPackageVersion = "Ausente";

    public string? GetManifestFilter(string type)
    {
        if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
            return PythonFilter;

        if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
            return DartFilter;

        if (type.Equals("typescript", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFilter;

        return null;
    }

    public PackageInstallInstructions GetInstallInstructions()
    {
        string appVersion = typeof(ProjectManager).Assembly.GetName().Version!.ToString(3);
        string dartInstruction = GetDartInstallInstruction();
        string pythonInstruction = GetPythonInstallInstruction(appVersion);
        string typeScriptInstruction = GetTypeScriptInstallInstruction();
        bool isDebugging = System.Diagnostics.Debugger.IsAttached;
#if DEBUG
        isDebugging = true;
#endif
        return new PackageInstallInstructions(Dart: dartInstruction, Python: pythonInstruction, TypeScript: typeScriptInstruction, IsDebugging: isDebugging);
    }

    public PackageVersion GetPackageVersion(string manifestPath, string type)
    {
        string appVersion = typeof(ProjectManager).Assembly.GetName().Version!.ToString(3);

        try
        {
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
                return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: ManifestFileNotFoundError);

            if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
                return GetPythonPackageVersion(manifestPath, appVersion);

            if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
                return GetDartPackageVersion(manifestPath, appVersion);

            if (type.Equals("typescript", StringComparison.OrdinalIgnoreCase))
                return GetTypeScriptPackageVersion(manifestPath, appVersion);

            return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: UnknownProjectTypeError);
        }
        catch (Exception exception)
        {
            return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: exception.Message);
        }
    }

    #endregion

    #region Python

    private const string PythonFilter = "Requisitos do Python (requirements.txt)|requirements.txt";

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
                var match = Regex.Match(line, @"amenolink-(\d+(?:\.\d+)+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string version = match.Groups[1].Value;
                    bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
                    return new PackageVersion(manifestPath, version, appVersion, isCompatible);
                }

                string remainder = line["amenolink".Length..].Trim();
                char[] versionPrefixes = ['=', '>', '<', '~', '!'];
                string simpleVersion = remainder.TrimStart(versionPrefixes).Trim();

                if (!string.IsNullOrEmpty(simpleVersion) && !simpleVersion.StartsWith('@'))
                {
                    bool isCompatible = string.Equals(simpleVersion, appVersion, StringComparison.OrdinalIgnoreCase);
                    return new PackageVersion(manifestPath, simpleVersion, appVersion, isCompatible);
                }

                return new PackageVersion(ManifestPath: manifestPath, Version: "*", AppVersion: appVersion);
            }
        }

        return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
    }

    private static string GetPythonInstallInstruction(string appVersion)
    {
        string wheelPath = Path.Combine(AppContext.BaseDirectory, "clients", "python", $"amenolink-{appVersion}-py3-none-any.whl").Replace('\\', '/');
        return $"pip install \"{wheelPath}\"\npip freeze > requirements.txt";
    }

    #endregion

    #region Dart

    private const string DartFilter = "Especificação do Pacote Dart (pubspec.yaml)|pubspec.yaml";

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
                {
                    string? lockVersion = GetDartLockVersion(manifestPath);
                    if (!string.IsNullOrEmpty(lockVersion))
                    {
                        bool lockCompatible = string.Equals(lockVersion, appVersion, StringComparison.OrdinalIgnoreCase);
                        return new PackageVersion(manifestPath, lockVersion, appVersion, lockCompatible);
                    }

                    return new PackageVersion(ManifestPath: manifestPath, Version: "*", AppVersion: appVersion);
                }

                bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
                return new PackageVersion(manifestPath, version, appVersion, isCompatible);
            }
        }

        return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
    }

    private static string? GetDartLockVersion(string manifestPath)
    {
        string? directory = Path.GetDirectoryName(manifestPath);
        if (string.IsNullOrEmpty(directory))
            return null;

        string lockPath = Path.Combine(directory, "pubspec.lock");
        if (!File.Exists(lockPath))
            return null;

        var lines = File.ReadAllLines(lockPath);
        bool inAmenolinkPackage = false;

        foreach (var rawLine in lines)
        {
            string trimmed = rawLine.Trim();

            if (rawLine.StartsWith("  ") && !rawLine.StartsWith("    "))
            {
                inAmenolinkPackage = trimmed.Equals("amenolink:", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inAmenolinkPackage && trimmed.StartsWith("version:", StringComparison.OrdinalIgnoreCase))
            {
                string version = trimmed["version:".Length..].Trim().Trim('"', '\'').Trim();
                if (!string.IsNullOrEmpty(version))
                    return version;
            }
        }

        return null;
    }

    private static string GetDartInstallInstruction()
    {
        string dartPackageDirectory = Path.Combine(AppContext.BaseDirectory, "clients", "dart", "amenolink").Replace('\\', '/');
        return $"  amenolink:\n    path: {dartPackageDirectory}";
    }

    #endregion

    #region TypeScript

    private const string TypeScriptFilter = "Manifesto do TypeScript (package.json)|package.json";

    private static PackageVersion GetTypeScriptPackageVersion(string manifestPath, string appVersion)
    {
        using var jsonDocument = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = jsonDocument.RootElement;

        string? foundVersion = null;
        if (rootElement.TryGetProperty("dependencies", out var dependencies) && dependencies.TryGetProperty("amenolink", out var dependencyVersion))
            foundVersion = dependencyVersion.GetString();
        else if (rootElement.TryGetProperty("devDependencies", out var devDependencies) && devDependencies.TryGetProperty("amenolink", out var devDependencyVersion))
            foundVersion = devDependencyVersion.GetString();

        if (foundVersion == null)
            return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);

        string cleanVersion = foundVersion.TrimStart('^', '~').Trim();
        bool isCompatible = string.Equals(cleanVersion, appVersion, StringComparison.OrdinalIgnoreCase);
        return new PackageVersion(manifestPath, cleanVersion, appVersion, isCompatible);
    }

    private static string GetTypeScriptInstallInstruction()
    {
        string typeScriptPackageDirectory = Path.Combine(AppContext.BaseDirectory, "clients", "typescript").Replace('\\', '/');
        return $"npm install \"{typeScriptPackageDirectory}\"";
    }

    #endregion
}
