/*
    Exemplos de tópicos. Será demonstrado como publicar e receber mensagens.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba TÓPICOS e adicione "example.topic" (sem aspas).
*/

using static AmenoLink.AmenoLinkClient;

namespace AmenoLinkExamples;

public static class TopicExample
{
    private const string TopicName = "example.topic";

    public static async Task Run()
    {
        Setup(appName: "Topic Example (C#)");

        // Duas interfaces para conectar ao mesmo tópico
        var listener = Topic<Talk>(TopicName);
        var sender = Topic<Talk>(TopicName);

        // Valida se os recursos estão configurados
        await EnsureReady();

        // A conexão é compartilhada por projeto,
        // recursos declarados antes ou após a usarão.
        await Connect();

        async void Reply(AmenoLink.TopicMessage<Talk> message)
        {
            await Task.Delay(500);
            var talk = message.Payload;
            if (talk == null || !talk.Reply)
                return;

            var replyTalk = new Talk(
                Author: "Bot",
                Text: "Não há ninguém por aqui. Você será desconectado.",
                Reply: false
            );

            // Ao enviar uma mensagem que é resposta a outra, envie a anterior para manter histórico
            // Caso haja loop de chamadas consecutivas, isso evitará loop infinito
            await sender.Publish(replyTalk, previous: message);
        }

        listener.Subscribe(ShowTalk);
        listener.Subscribe(Reply);

        var talkMessage = new Talk(Author: "Gary Stu", Text: "Olá!", Reply: false);
        await sender.Publish(talkMessage);
        await Task.Delay(1000);

        talkMessage = new Talk(Author: "Gary Stu", Text: "Alguém por aí?", Reply: true);
        await sender.Publish(talkMessage);
        await Task.Delay(1000);

        // Ao usar dispose, elimina todas as inscrições daquela instância
        listener.Dispose();

        talkMessage = new Talk(Author: "Gary Stu", Text: "Por quê???", Reply: false);
        // Não há inscritos para receber
        await sender.Publish(talkMessage);
        await Task.Delay(500);

        sender.Dispose();
        await Disconnect();
    }

    private static void ShowTalk(AmenoLink.TopicMessage<Talk> message)
    {
        var talk = message.Payload;
        if (talk != null)
            Console.WriteLine($"{talk.Author}: {talk.Text}");
    }
}
