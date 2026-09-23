/*
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
*/

using System.Text.Json;
using AmenoLink;
using static AmenoLink.AmenoLinkClient;

namespace AmenoLinkExamples;

public static class CacheExample
{
    private const string CacheGroup = "example.cache";

    public static async Task Run()
    {
        Setup(appName: "Cache Example (C#)");

        // Declaração do grupo de cache
        var exampleCache = Cache(CacheGroup);

        // Valida se os recursos estão configurados
        await EnsureReady();

        await BasicExample(exampleCache);
        await WatcherExample(exampleCache);
    }

    private static async Task BasicExample(ICache exampleCache)
    {
        var garyStu = new User(Name: "Gary Stu", BirthDate: "20/01/2001");
        var marySue = new User(Name: "Mary Sue", BirthDate: "19/08/1988");

        // Valor não definido retorna null
        var user = await exampleCache.Get<User>("gary");
        Console.WriteLine($"get: {JsonSerializer.Serialize(user, JsonOptions)}\n");

        // Caso não exista, será criado
        user = await exampleCache.GetOrCreate("gary", () => garyStu);
        Console.WriteLine($"getOrCreate: {JsonSerializer.Serialize(user, JsonOptions)}");
        Console.WriteLine($"get: {JsonSerializer.Serialize(user, JsonOptions)}\n");

        // Definir e excluir valores
        await exampleCache.Set("mary", marySue);
        user = await exampleCache.Get<User>("mary");
        Console.WriteLine($"set: {JsonSerializer.Serialize(user, JsonOptions)}\n");

        await exampleCache.Delete("mary");
        user = await exampleCache.Get<User>("mary");
        Console.WriteLine($"delete: {JsonSerializer.Serialize(user, JsonOptions)}\n");

        // Obter todos os registros:
        await exampleCache.Set("mary", marySue);
        await exampleCache.Set("port", 13545);
        await exampleCache.Set("true", true);
        var entries = await exampleCache.All();
        Console.WriteLine($"all: {JsonSerializer.Serialize(entries, JsonOptions)}\n");

        // Remover todos os registros:
        await exampleCache.Clear();
        entries = await exampleCache.All();
        Console.WriteLine($"clear: {JsonSerializer.Serialize(entries, JsonOptions)}\n");
    }

    private static async Task WatcherExample(ICache exampleCache)
    {
        void ValueChanged(string key, object? value)
        {
            Console.WriteLine($"[{key}]: {JsonSerializer.Serialize(value, JsonOptions)}");
        }

        void UserChanged(User? user)
        {
            Console.WriteLine($"> User: {JsonSerializer.Serialize(user, JsonOptions)}");
        }

        var joe = new User(Name: "Average Joe", BirthDate: "12/07/2010");
        var jane = new User(Name: "Average Jane", BirthDate: "07/12/2010");

        await Connect();

        // Esse é o observador de alterações
        var watcher = exampleCache.Watch();

        // Pode monitorar todas as alterações
        watcher.All(ValueChanged);
        await exampleCache.Set("total", 5);
        await exampleCache.Set("checked", false);

        // Ou monitorar uma chave específica
        watcher.Key<User>("user", UserChanged);
        await exampleCache.Set("user", joe);
        await exampleCache.Set("user", jane);

        // Ao excluir valores, eles virão nulos
        await exampleCache.Delete("user");
        await exampleCache.Clear();
        await Task.Delay(1000);

        // No fim, descarte o watcher para encerrar as inscrições
        watcher.Dispose();
        await exampleCache.Set("total", 9);
        await Task.Delay(1000);
        await exampleCache.Clear();
        await Disconnect();
    }
}
