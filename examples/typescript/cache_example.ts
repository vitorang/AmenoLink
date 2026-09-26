/*
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
*/

import { cache, connection, ensureReady, setup, Cache } from 'amenolink';
import { User } from './dtos';

const CACHE_GROUP = 'example.cache';

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function basicExample(exampleCache: Cache): Promise<void> {
    const garyStu: User = { name: 'Gary Stu', birthDate: '20/01/2001' };
    const marySue: User = { name: 'Mary Sue', birthDate: '19/08/1988' };

    // Valor não definido retorna null
    let user = await exampleCache.get<User>('gary');
    console.log(`get: ${JSON.stringify(user)}\n`);

    // Caso não exista, será criado
    user = await exampleCache.getOrCreate<User>('gary', () => garyStu);
    console.log(`getOrCreate: ${JSON.stringify(user)}`);
    console.log(`get: ${JSON.stringify(user)}\n`);

    // Definir e excluir valores
    await exampleCache.set<User>('mary', marySue);
    user = await exampleCache.get<User>('mary');
    console.log(`set: ${JSON.stringify(user)}\n`);

    await exampleCache.delete('mary');
    user = await exampleCache.get<User>('mary');
    console.log(`delete: ${JSON.stringify(user)}\n`);

    // Obter todos os registros:
    await exampleCache.set<User>('mary', marySue);
    await exampleCache.set('port', 13545);
    await exampleCache.set('true', true);
    let entries = await exampleCache.all();
    console.log(`all: ${JSON.stringify(entries)}\n`);

    // Remover todos os registros:
    await exampleCache.clear();
    entries = await exampleCache.all();
    console.log(`clear: ${JSON.stringify(entries)}\n`);
}

async function watcherExample(exampleCache: Cache): Promise<void> {
    function valueChanged(key: string, value: any): void {
        console.log(`[${key}]: ${JSON.stringify(value)}`);
    }

    function userChanged(user: User | null): void {
        console.log(`> User: ${JSON.stringify(user)}`);
    }

    const joe: User = { name: 'Average Joe', birthDate: '12/07/2010' };
    const jane: User = { name: 'Average Jane', birthDate: '07/12/2010' };

    await connection.connect();

    // Esse é o observador de alterações
    const watcher = exampleCache.watch();

    // Pode monitorar todas as alterações
    watcher.all(valueChanged);
    await exampleCache.set('total', 5);
    await exampleCache.set('checked', false);

    // Ou monitorar uma chave específica
    watcher.key<User>('user', userChanged);
    await exampleCache.set<User>('user', joe);
    await exampleCache.set<User>('user', jane);

    // Ao excluir valores, eles virão nulos
    await exampleCache.delete('user');
    await exampleCache.clear();
    await sleep(1000);

    // No fim, descarte o watcher para encerrar as inscrições
    watcher.dispose();
    await exampleCache.set('total', 9);
    await sleep(1000);
    await exampleCache.clear();
    await connection.disconnect();
    connection.unsubscribeAll();
}

async function main(): Promise<void> {
    setup({ appName: 'Cache Example (TypeScript)' });

    // Declaração do grupo de cache
    const exampleCache = cache(CACHE_GROUP);

    // Valida se os recursos estão configurados
    await ensureReady();

    await basicExample(exampleCache);
    await watcherExample(exampleCache);
}

main().catch(console.error);
