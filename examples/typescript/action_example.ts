/*
    Exemplos de execução de Action. Será demonstrado como usar recursos de requisição e filas.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink e vá na aba PROGRAMAS. Adicione action_server.ts
        e adicione "example.action" (sem aspas) na seção Ações.

    - Na aba TÓPICOS, adicione "example.action".
*/

import { action, connect, disconnect, ensureReady, setup, topic, ActionResponse, TopicMessage } from 'amenolink';
import { User, UserAstrology } from './dtos';

const ACTION_ROUTE = 'example.action';

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function onStatusChange(status: any): void {
    console.log(`Estado da conexão: ${status}`);
}

function formatUserAstrology(ua?: UserAstrology | null): string {
    if (!ua)
        return '';

    return `\n\t${ua.name} de ${ua.sign}\n\tNascido em ${ua.weekDay}, ${ua.birthDate}\n`;
}

// Actions adicionam ActionResponse no payload de TopicMessage
function onMessageReceived(message: TopicMessage<ActionResponse<UserAstrology>>): void {
    const response = message.payload;
    if (!response)
        return;

    console.log(`Mensagem do tópico: \n\tLogs: ${JSON.stringify(response.logs)}${formatUserAstrology(response.result)}`);
}

async function main(): Promise<void> {
    setup({ appName: 'Action Example (TypeScript)' });

    // Declaração dos recursos utilizados
    const exampleAction = action(ACTION_ROUTE);
    const actionTopic = topic<ActionResponse<UserAstrology>>(ACTION_ROUTE);

    // Valida se os recursos estão configurados
    await ensureReady();

    const garyStu: User = { name: 'Gary Stu', birthDate: '20/01/2001' };
    const marySue: User = { name: 'Mary Sue', birthDate: '19/08/1988' };

    // Abre uma conexão persistente
    await connect({ onStatusChange });

    // Os resultados são publicados no tópico com mesmo nome da ação
    actionTopic.subscribe(onMessageReceived);

    // Com request poderá obter resultados de forma síncrona
    // e não precisará usar tópico ou conexão persistente.
    // Porém, o resultado será publicado no tópico!
    const astrology = await exampleAction.request<UserAstrology>(garyStu);
    console.log(`Resposta de requisição: ${formatUserAstrology(astrology)}`);
    await sleep(500);

    // Ou executar de forma assíncrona se não precisar do resultado ou o processamento for lento
    await exampleAction.queue(marySue);
    await sleep(1000);

    // Desativa conexão do tópico. Após isso, ele não poderá ser usado
    actionTopic.dispose();

    // Fecha a conexão. É necessário para o programa se encerrar.
    await disconnect();
}

main().catch(console.error);
