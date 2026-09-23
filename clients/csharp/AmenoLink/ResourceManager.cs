namespace AmenoLink;

public class ResourceManager
{
    public static string PackageVersion =>
        typeof(ResourceManager).Assembly.GetName().Version!.ToString(3);

    public HashSet<string> Actions { get; } = [];
    public HashSet<string> Caches { get; } = [];
    public HashSet<string> Topics { get; } = [];

    public async Task EnsureReady()
    {
        var resources = new Resources(
            Actions: [.. Actions],
            Caches: [.. Caches],
            Topics: [.. Topics],
            Version: PackageVersion
        );

        string url = $"{AmenoLinkClient.Settings.OriginUrl}/api/resources/missing";
        var missingResources = await AmenoLinkClient.PostJson<Resources>(url, resources);

        if (!string.Equals(missingResources.Version, PackageVersion, StringComparison.OrdinalIgnoreCase))
            throw new AmenoException($"Versão incompatível do AmenoLink. Host: {missingResources.Version}, Cliente: {PackageVersion}.");

        var missingItems = new List<string>();

        if (missingResources.Actions != null && missingResources.Actions.Length > 0)
            missingItems.Add($"Actions: {string.Join(", ", missingResources.Actions)}");

        if (missingResources.Caches != null && missingResources.Caches.Length > 0)
            missingItems.Add($"Caches: {string.Join(", ", missingResources.Caches)}");

        if (missingResources.Topics != null && missingResources.Topics.Length > 0)
            missingItems.Add($"Topics: {string.Join(", ", missingResources.Topics)}");

        if (missingItems.Count > 0)
        {
            string details = string.Join("\n", missingItems);
            throw new AmenoException($"Recursos ausentes no AmenoLink:\n{details}");
        }
    }
}
