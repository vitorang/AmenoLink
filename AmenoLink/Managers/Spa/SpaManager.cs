using AmenoLink.Interfaces.Managers.Spa;
using AmenoLink.Managers.Configuration;

namespace AmenoLink.Managers.Spa;

internal class SpaManager : ISpaManager
{
    private readonly Dictionary<string, SpaConfig> spaMap = new(StringComparer.Ordinal);

    public void LoadConfigurations()
    {
        var configs = ConfigPathProvider.Spa.LoadConfigs();

        lock (spaMap)
        {
            spaMap.Clear();

            foreach (var config in configs)
            {
                if (string.IsNullOrWhiteSpace(config.Route))
                    continue;

                string route = config.Route.Trim();
                spaMap[route] = config with { Route = route };
            }
        }
    }

    public bool TryGetSpa(string route, out SpaConfig spaConfig)
    {
        lock (spaMap)
        {
            return spaMap.TryGetValue(route, out spaConfig!);
        }
    }

    public string? ResolveFile(string route, string relativePath, out bool isIndexFallback)
    {
        isIndexFallback = false;

        if (!TryGetSpa(route, out var config))
            return null;

        if (string.IsNullOrWhiteSpace(config.RootPath) || !Directory.Exists(config.RootPath))
            return null;

        string rootFullPath = Path.GetFullPath(config.RootPath);
        string normalizedRelativePath = (relativePath ?? string.Empty).TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            string candidateFilePath = Path.GetFullPath(Path.Combine(rootFullPath, normalizedRelativePath));

            if (!candidateFilePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
                return null;

            if (File.Exists(candidateFilePath))
                return candidateFilePath;

            string firstSegment = normalizedRelativePath.Split(Path.DirectorySeparatorChar)[0];
            string possibleDirectory = Path.Combine(rootFullPath, firstSegment);

            if (Directory.Exists(possibleDirectory))
                return null;
        }

        string indexFileName = (config.IndexFile ?? string.Empty).TrimStart('/', '\\');
        if (string.IsNullOrWhiteSpace(indexFileName))
            return null;

        string indexFilePath = Path.GetFullPath(Path.Combine(rootFullPath, indexFileName));

        if (!indexFilePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            return null;

        if (File.Exists(indexFilePath))
        {
            isIndexFallback = true;
            return indexFilePath;
        }

        return null;
    }

    public async Task<string?> GetIndexHtml(string route, string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        string html = await File.ReadAllTextAsync(filePath);
        var parser = new AngleSharp.Html.Parser.HtmlParser();
        var document = parser.ParseDocument(html);

        var baseElement = document.QuerySelector("base");
        if (baseElement is not null)
            baseElement.SetAttribute("href", $"/spa/{route}/");
        else
        {
            var head = document.Head;
            if (head is not null)
            {
                var newBase = document.CreateElement("base");
                newBase.SetAttribute("href", $"/spa/{route}/");
                head.InsertBefore(newBase, head.FirstChild);
            }
        }

        return $"<!DOCTYPE html>\n{document.DocumentElement.OuterHtml}";
    }
}
