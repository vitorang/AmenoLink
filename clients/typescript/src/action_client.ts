import { ulid } from 'ulid';
import { ActionRequest } from './dtos';
import { resourceManager } from './resource_manager';
import { clientSetup, postJson, AmenoException } from './shared';

export class Action {
    public readonly name: string;

    constructor(name: string) {
        this.name = name;
    }

    public async request<T>(payload?: unknown): Promise<T> {
        const requestDto: ActionRequest<unknown> = {
            id: ulid(),
            createdAt: new Date().toISOString(),
            type: 'ActionRequest',
            route: this.name,
            payload,
            appName: clientSetup.appName
        };

        const url = `${clientSetup.originUrl}/api/request`;
        const responseData = await postJson<any>(url, requestDto);

        if (responseData?.success !== true) {
            const errorInfo = responseData?.error;
            let errorMessage = 'Erro desconhecido ao executar ação.';

            if (typeof errorInfo === 'object' && errorInfo !== null && errorInfo.message)
                errorMessage = errorInfo.message;
            else if (typeof responseData?.errorMessage === 'string' && responseData.errorMessage)
                errorMessage = responseData.errorMessage;

            throw new AmenoException(errorMessage);
        }

        const responseValue = 'result' in responseData ? responseData.result : responseData.response;
        return responseValue as T;
    }

    public async queue(payload?: unknown): Promise<void> {
        const requestDto: ActionRequest<unknown> = {
            id: ulid(),
            createdAt: new Date().toISOString(),
            type: 'ActionRequest',
            route: this.name,
            payload,
            appName: clientSetup.appName
        };

        const url = `${clientSetup.originUrl}/api/queue`;
        await postJson(url, requestDto);
    }
}

export function action(name: string): Action {
    resourceManager.actions.add(name);
    return new Action(name);
}
