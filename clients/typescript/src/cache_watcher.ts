import { ICacheWatcher } from './cache_manager';
import { connectionManager } from './connection_manager';
import { AmenoException } from './shared';

export type CacheAllHandler = (key: string, value: any) => void;
export type CacheKeyHandler<T> = (value: T | null) => void;

interface KeySubscription {
    handler: (rawValue: any) => void;
}

export class CacheWatcher implements ICacheWatcher {
    public readonly group: string;
    private disposed = false;
    private readonly allHandlers: Set<CacheAllHandler> = new Set();
    private readonly keyHandlers: Map<string, Set<KeySubscription>> = new Map();

    constructor(group: string) {
        this.group = group;
    }

    public all(handler: CacheAllHandler): void {
        this.ensureNotDisposed();
        this.allHandlers.add(handler);
        connectionManager.cacheManager.subscribeWatcher(this);
    }

    public key<T>(key: string, handler: CacheKeyHandler<T>): void {
        this.ensureNotDisposed();
        if (!this.keyHandlers.has(key))
            this.keyHandlers.set(key, new Set());

        const subscriptions = this.keyHandlers.get(key)!;
        const wrapper = (rawValue: any): void => {
            handler(rawValue as T | null);
        };

        subscriptions.add({ handler: wrapper });
        connectionManager.cacheManager.subscribeWatcher(this);
    }

    public dispose(): void {
        this.ensureNotDisposed();
        this.disposed = true;
        this.allHandlers.clear();
        this.keyHandlers.clear();
        connectionManager.cacheManager.unsubscribeWatcher(this);
    }

    public dispatchValueChanged(key: string, rawValue: any): void {
        if (this.disposed)
            return;

        for (const handler of Array.from(this.allHandlers))
            handler(key, rawValue);

        const subscriptions = this.keyHandlers.get(key);
        if (subscriptions)
            for (const subscription of Array.from(subscriptions))
                subscription.handler(rawValue);
    }

    private ensureNotDisposed(): void {
        if (this.disposed)
            throw new AmenoException(`O observador do grupo de cache '${this.group}' já foi descartado (disposed).`);
    }
}
