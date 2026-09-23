namespace AmenoLinkExamples;

public static class Program
{
    public static async Task Main(string[] args)
    {
        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "topic";

        var task = command switch
        {
            "topic" => TopicExample.Run(),
            "cache" => CacheExample.Run(),
            "action" => ActionExample.Run(),
            _ => Task.Run(() => Console.WriteLine($"Comando desconhecido: '{command}'. Use 'topic', 'cache' ou 'action'."))
        };

        await task;
    }
}
