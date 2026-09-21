import * as readline from 'node:readline';
import { ActionRequest } from './dtos';
import { clientSetup, AmenoException } from './shared';

const ON_STARTUP_SUCCESS = '[AmenoLink.StartupSuccess]';
const ON_ACTION_SUCCESS = '[AmenoLink.ActionSuccess]';
const ON_ACTION_ERROR = '[AmenoLink.ActionError]';
const ON_ACTION_LOGGED = '[AmenoLink.ActionLog]';

export type ActionHandler<T, R> = (input: T) => Promise<R> | R;

export interface ActionRoute {
    route: string;
    handler: ActionHandler<any, any>;
}

export class ActionContext {
    public readonly request: ActionRequest<unknown>;

    constructor(request: ActionRequest<unknown>) {
        this.request = request;
    }

    public log(message: string): void {
        sendMessage(ON_ACTION_LOGGED, message);
    }
}

export class ActionRouter {
    private readonly routes: ActionRoute[] = [];

    public add<T, R>(route: string, handler: ActionHandler<T, R>): void {
        this.routes.push({ route, handler });
    }

    private async execute(request: ActionRequest<any>): Promise<string> {
        for (const route of this.routes) {
            if (route.route === request.route) {
                const rawResult = await route.handler(request.payload);
                return this.formatResult(rawResult);
            }
        }
        throw new AmenoException(`Rota '${request.route}' não encontrada`);
    }

    private formatResult(rawResult: any): string {
        return JSON.stringify(rawResult ?? null);
    }

    public serve(): void {
        sendMessage(ON_STARTUP_SUCCESS, clientSetup.appName);

        const rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout,
            terminal: false
        });

        rl.on('line', async (line: string) => {
            const trimmedLine = line.trim();
            if (!trimmedLine)
                return;

            try {
                const decodedJson = Buffer.from(trimmedLine, 'base64').toString('utf-8');
                const request: ActionRequest<any> = JSON.parse(decodedJson);

                currentAction = new ActionContext(request);
                const result = await this.execute(request);
                sendMessage(ON_ACTION_SUCCESS, result);
            } catch (error: any) {
                sendMessage(ON_ACTION_ERROR, error?.message ?? String(error));
            } finally {
                currentAction = null;
            }
        });

        rl.on('error', (error: any) => {
            sendMessage(ON_ACTION_ERROR, error?.message ?? String(error));
        });
    }
}

export const actions = new ActionRouter();
let currentAction: ActionContext | null = null;

export function actionContext(): ActionContext {
    if (!currentAction)
        throw new Error('Nenhuma ação está em execução no momento.');

    return currentAction;
}

function sendMessage(prefix: string, message: string): void {
    const payload = Buffer.from(message, 'utf-8').toString('base64');
    process.stdout.write(`${prefix}${payload}\n`);
}
