using AmenoLink.Interfaces.Managers.Configuration;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace AmenoLink.Managers.Configuration;

internal class ProjectManager : IProjectManager
{
    #region Common

    private const string ManifestFileNotFoundError = "Arquivo de manifesto não foi encontrado";
    private const string UnknownProjectTypeError = "Tipo de projeto desconhecido";
    private const string MissingPackageVersion = "Ausente";

    public string? GetManifestFilter(string type)
    {
        if (type.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            return CSharpFilter;

        if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
            return DartFilter;

        if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
            return PythonFilter;

        if (type.Equals("typescript", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFilter;

        return null;
    }

    public PackageInstallInstructions GetInstallInstructions()
    {
        string appVersion = typeof(ProjectManager).Assembly.GetName().Version!.ToString(3);
        string csharpInstruction = GetCSharpInstallInstruction();
        string dartInstruction = GetDartInstallInstruction(appVersion);
        string pythonInstruction = GetPythonInstallInstruction(appVersion);
        string typeScriptInstruction = GetTypeScriptInstallInstruction(appVersion);
        bool isDebugging = System.Diagnostics.Debugger.IsAttached;
#if DEBUG
        isDebugging = true;
#endif
        return new PackageInstallInstructions(
            CSharp: csharpInstruction,
            Dart: dartInstruction,
            Python: pythonInstruction,
            TypeScript: typeScriptInstruction,
            IsDebugging: isDebugging
        );
    }

    public PackageVersion GetPackageVersion(string manifestPath, string type)
    {
        string appVersion = typeof(ProjectManager).Assembly.GetName().Version!.ToString(3);

        try
        {
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
                return new PackageVersion(ManifestPath: manifestPath, AppVersion: appVersion, ErrorReason: ManifestFileNotFoundError);

            if (type.Equals("csharp", StringComparison.OrdinalIgnoreCase))
                return GetCSharpPackageVersion(manifestPath, appVersion);

            if (type.Equals("dart", StringComparison.OrdinalIgnoreCase))
                return GetDartPackageVersion(manifestPath, appVersion);

            if (type.Equals("python", StringComparison.OrdinalIgnoreCase))
                return GetPythonPackageVersion(manifestPath, appVersion);

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

    #region CSharp

    private const string CSharpFilter = "Projeto C# (*.csproj)|*.csproj";

    private static PackageVersion GetCSharpPackageVersion(string manifestPath, string appVersion)
    {
        string content = File.ReadAllText(manifestPath);

        var match = Regex.Match(content, @"<PackageReference\s+Include=""AmenoLink""\s+Version=""([^""]+)""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string version = match.Groups[1].Value.Trim();
            bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
            return new PackageVersion(manifestPath, version, appVersion, isCompatible);
        }

        return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
    }

    private static string GetCSharpInstallInstruction()
    {
        string csharpPackageDirectory = Path.Combine(AppContext.BaseDirectory, "clients", "csharp").Replace('\\', '/');
        return $"dotnet add package AmenoLink -s \"{csharpPackageDirectory}\"";
    }

    #endregion

    #region Dart

    private const string DartFilter = "Especificação do Pacote Dart (pubspec.yaml)|pubspec.yaml";

    private static PackageVersion GetDartPackageVersion(string manifestPath, string appVersion)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            using var reader = new StreamReader(manifestPath);
            var yamlObject = deserializer.Deserialize<Dictionary<object, object>>(reader);

            if (yamlObject == null)
                return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);

            if (!yamlObject.TryGetValue("dependencies", out var dependenciesObj) || dependenciesObj is not Dictionary<object, object> dependenciesDict)
                return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);

            if (!dependenciesDict.TryGetValue("amenolink", out var amenolinkObj) || amenolinkObj == null)
                return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);

            string? version = null;

            if (amenolinkObj is string inlineVersion)
                version = inlineVersion.Trim().Trim('^', '"', '\'').Trim();
            else if (amenolinkObj is Dictionary<object, object> amenolinkDict)
            {
                if (amenolinkDict.TryGetValue("version", out var versionObj) && versionObj is string blockVersion)
                    version = blockVersion.Trim().Trim('^', '"', '\'').Trim();
            }

            if (string.IsNullOrEmpty(version))
                return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);

            bool isCompatible = string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase);
            return new PackageVersion(manifestPath, version, appVersion, isCompatible);
        }
        catch
        {
            return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
        }
    }

    private static string GetDartInstallInstruction(string appVersion)
    {
        string dartPackageDirectory = Path.Combine(AppContext.BaseDirectory, "clients", "dart", "amenolink").Replace('\\', '/');
        return $"  amenolink:\n    version: ^{appVersion}\n    path: {dartPackageDirectory}";
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

                return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
            }
        }

        return new PackageVersion(ManifestPath: manifestPath, Version: MissingPackageVersion, AppVersion: appVersion, IsCompatible: false);
    }

    private static string GetPythonInstallInstruction(string appVersion)
    {
        string wheelPath = Path.Combine(AppContext.BaseDirectory, "clients", "python", $"amenolink-{appVersion}-py3-none-any.whl").Replace('\\', '/');
        return $"./venv/Scripts/python -m pip install \"{wheelPath}\"\n./venv/Scripts/python -m pip freeze | Out-File -Encoding utf8 requirements.txt";
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

        var tarballMatch = Regex.Match(foundVersion, @"amenolink-(\d+(?:\.\d+)+)\.tgz", RegexOptions.IgnoreCase);
        if (tarballMatch.Success)
        {
            string tarballVersion = tarballMatch.Groups[1].Value;
            bool isCompatible = string.Equals(tarballVersion, appVersion, StringComparison.OrdinalIgnoreCase);
            return new PackageVersion(manifestPath, tarballVersion, appVersion, isCompatible);
        }

        string cleanVersion = foundVersion.TrimStart('^', '~').Trim();
        bool isCleanCompatible = string.Equals(cleanVersion, appVersion, StringComparison.OrdinalIgnoreCase);
        return new PackageVersion(manifestPath, cleanVersion, appVersion, isCleanCompatible);
    }

    private static string GetTypeScriptInstallInstruction(string appVersion)
    {
        string typeScriptPackageTarball = Path.Combine(AppContext.BaseDirectory, "clients", "typescript", $"amenolink-{appVersion}.tgz").Replace('\\', '/');
        return $"npm install --save \"{typeScriptPackageTarball}\"";
    }

    #endregion
}
