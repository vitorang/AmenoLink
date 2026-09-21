/*
    Exemplos de tópicos. Será demonstrado como publicar e receber mensagens.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba TÓPICOS e adicione "example.topic" (sem aspas).
*/

import { connect, disconnect, ensureReady, setup, topic, TopicMessage } from 'amenolink';
import { Talk } from './dtos';

const TOPIC_NAME = 'example.topic';

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function showTalk(message: TopicMessage<Talk>): void {
    const talk = message.payload;
    if (talk)
        console.log(`${talk.author}: ${talk.text}`);
}

async function main(): Promise<void> {
    setup({ appName: 'Topic Example (TypeScript)' });

    // Duas interfaces para conectar ao mesmo tópico
    const listener = topic<Talk>(TOPIC_NAME);
    const sender = topic<Talk>(TOPIC_NAME);

    // Valida se os recursos estão configurados
    await ensureReady();

    // A conexão é compartilhada por projeto,
    // recursos declarados antes ou após a usarão.
    await connect();

    async function reply(message: TopicMessage<Talk>): Promise<void> {
        await sleep(500);
        const talk = message.payload;
        if (!talk || !talk.reply)
            return;

        const replyTalk: Talk = {
            author: 'Bot',
            text: 'Não há ninguém por aqui. Você será desconectado.',
            reply: false
        };

        // Ao enviar uma mensagem que é resposta a outra, envie a anterior para manter histórico
        // Caso haja loop de chamadas consecutivas, isso evitará loop infinito
        await sender.publish(replyTalk, { previous: message });
    }

    listener.subscribe(showTalk);
    listener.subscribe(reply);

    let talk: Talk = { author: 'Gary Stu', text: 'Olá!', reply: false };
    await sender.publish(talk);
    await sleep(1000);

    talk = { author: 'Gary Stu', text: 'Alguém por aí?', reply: true };
    await sender.publish(talk);
    await sleep(1000);

    // Ao usar dispose, elimina todas as inscrições daquela instância
    listener.dispose();

    talk = { author: 'Gary Stu', text: 'Por quê???', reply: false };
    // Não há inscritos para receber
    await sender.publish(talk);
    await sleep(500);

    sender.dispose();
    await disconnect();
}

main().catch(console.error);
