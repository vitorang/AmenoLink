/*
    Exemplos de execução de Action. Será demonstrado como usar recursos de requisição e filas.

    CONFIGURAÇÃO
    - No projeto do ActionServer, gere o executável publicando o projeto:
        dotnet publish -c Release

    - Execute o programa AmenoLink e vá na aba PROGRAMAS. Adicione o executável gerado
      e adicione "example.action" (sem aspas) na seção Ações.

    - Na aba TÓPICOS, adicione "example.action".
*/

using System.Text.Json;
using AmenoLink;
using static AmenoLink.AmenoLinkClient;

namespace AmenoLinkExamples;

public static class ActionExample
{
    private const string ActionRoute = "example.action";

    public static async Task Run()
    {
        Setup(appName: "Action Example (C#)");

        // Declaração dos recursos utilizados
        var exampleAction = Action(ActionRoute);
        var actionTopic = Topic<ActionResponse<UserAstrology>>(ActionRoute);

        // Valida se os recursos estão configurados
        await EnsureReady();

        var garyStu = new User(Name: "Gary Stu", BirthDate: "20/01/2001");
        var marySue = new User(Name: "Mary Sue", BirthDate: "19/08/1988");

        void OnStatusChange(ConnectionStatus status) => Console.WriteLine($"Estado da conexão: {status}");

        // Inscreve para receber eventos de status da conexão
        Connection.Subscribe(OnStatusChange);

        // Abre uma conexão persistente
        await Connection.Connect();

        // Os resultados são publicados no tópico com mesmo nome da ação
        actionTopic.Subscribe(OnMessageReceived);

        // Com request poderá obter resultados de forma síncrona
        // e não precisará usar tópico ou conexão persistente.
        // Porém, o resultado será publicado no tópico!
        var astrology = await exampleAction.Request<UserAstrology>(garyStu);
        Console.WriteLine($"Resposta de requisição: {FormatUserAstrology(astrology)}");
        await Task.Delay(500);

        // Ou executar de forma assíncrona se não precisar do resultado ou o processamento for lento
        await exampleAction.Queue(marySue);
        await Task.Delay(1000);

        // Desativa conexão do tópico. Após isso, ele não poderá ser usado
        actionTopic.Dispose();

        // Fecha a conexão.
        await Connection.Disconnect();

        // Pode desinscrever um evento
        Connection.Unsubscribe(OnStatusChange);

        // Ou desinscrever todos globalmente
        Connection.UnsubscribeAll();
    }


    private static string FormatUserAstrology(UserAstrology? ua)
    {
        if (ua == null)
            return string.Empty;

        return $"\n\t{ua.Name} de {ua.Sign}\n\tNascido em {ua.WeekDay}, {ua.BirthDate}\n";
    }

    // Actions adicionam ActionResponse no payload de TopicMessage
    private static Task OnMessageReceived(TopicMessage<ActionResponse<UserAstrology>> message)
    {
        var response = message.Payload;
        if (response != null)
        {
            string logs = JsonSerializer.Serialize(response.Logs, JsonOptions);
            Console.WriteLine($"Mensagem do tópico: \n\tLogs: {logs}{FormatUserAstrology(response.Result)}");
        }

        return Task.CompletedTask;
    }
}
