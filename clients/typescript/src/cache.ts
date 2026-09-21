import { CacheWatcher } from './cache_watcher';
import { resourceManager } from './resource_manager';
import { clientSetup, AmenoException } from './shared';

export class Cache {
    public readonly group: string;

    constructor(group: string) {
        this.group = group;
    }

    public watch(): CacheWatcher {
        return new CacheWatcher(this.group);
    }

    public async get<T>(key: string): Promise<T | null> {
        const rawValue = await this.request('GET', this.buildCacheUrl(key));
        if (rawValue === null || rawValue === undefined)
            return null;

        return rawValue as T;
    }

    public async set<T>(key: string, value: T): Promise<void> {
        await this.request('POST', this.buildCacheUrl(key), value);
    }

    public async getOrCreate<T>(key: string, creator: () => Promise<T> | T): Promise<T> {
        const cachedValue = await this.get<T>(key);
        if (cachedValue !== null && cachedValue !== undefined)
            return cachedValue;

        const createdValue = await creator();
        await this.set(key, createdValue);
        return createdValue;
    }

    public async all(): Promise<Record<string, any>> {
        const responseData = await this.request('GET', this.buildCacheAllUrl());
        if (typeof responseData !== 'object' || responseData === null)
            throw new AmenoException('Resposta inesperada da API de cache');

        return responseData;
    }

    public async clear(): Promise<void> {
        await this.request('DELETE', this.buildCacheAllUrl());
    }

    public async delete(key: string): Promise<void> {
        await this.request('DELETE', this.buildCacheUrl(key));
    }

    private buildCacheUrl(key: string): string {
        const encodedGroup = encodeURIComponent(this.group);
        const encodedKey = encodeURIComponent(key);
        return `${clientSetup.originUrl}/api/cache?groupName=${encodedGroup}&key=${encodedKey}`;
    }

    private buildCacheAllUrl(): string {
        const encodedGroup = encodeURIComponent(this.group);
        return `${clientSetup.originUrl}/api/cache/all?groupName=${encodedGroup}`;
    }

    private async request(method: string, url: string, data?: any): Promise<any> {
        try {
            const headers: Record<string, string> = {};
            let body: string | undefined;

            if (data !== undefined) {
                headers['Content-Type'] = 'application/json';
                body = JSON.stringify(data);
            }

            const response = await fetch(url, {
                method,
                headers,
                body
            });

            if (!response.ok)
                throw new AmenoException(`Status HTTP inesperado: ${response.status}`);

            const text = await response.text();
            if (!text || text.trim() === '')
                return null;

            return JSON.parse(text);
        } catch (error: any) {
            if (error instanceof AmenoException)
                throw error;

            throw new AmenoException(`Erro na operação de cache: ${error.message}`);
        }
    }
}

export function cache(groupName: string): Cache {
    resourceManager.caches.add(groupName);
    return new Cache(groupName);
}
