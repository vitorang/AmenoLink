namespace AmenoLink.Interfaces.Managers.Spa;

public interface ISpaManager
{
    void LoadConfigurations();
    bool TryGetSpa(string route, out SpaConfig spaConfig);
    string? ResolveFile(string route, string relativePath, out bool isIndexFallback);
    Task<string?> GetIndexHtml(string route, string filePath);
}
