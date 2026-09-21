export class AmenoException extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'AmenoException';
    }
}

export interface ClientSetup {
    originUrl: string;
    appName: string;
}

export const clientSetup: ClientSetup = {
    originUrl: 'http://localhost:13545',
    appName: ''
};

export function setup(options: { originUrl?: string; appName?: string }): void {
    if (options.originUrl !== undefined)
        clientSetup.originUrl = options.originUrl.replace(/\/+$/, '');

    if (options.appName !== undefined)
        clientSetup.appName = options.appName;
}

export async function postJson<T>(url: string, data: any): Promise<T> {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });

    const text = await response.text();
    if (!text || text.trim() === '')
        return {} as T;

    return JSON.parse(text) as T;
}

