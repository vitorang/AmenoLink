import { IConnectionManager } from './topic_manager';

export interface ICacheWatcher {
    readonly group: string;
    dispatchValueChanged(key: string, value: any): void;
}

export class CacheManager {
    private readonly connectionManager: IConnectionManager;
    public readonly cacheMap: Map<string, Set<ICacheWatcher>> = new Map();

    constructor(connectionManager: IConnectionManager) {
        this.connectionManager = connectionManager;
    }

    public subscribeWatcher(watcherInstance: ICacheWatcher): void {
        const groupName = watcherInstance.group;
        if (!groupName)
            return;

        if (!this.cacheMap.has(groupName))
            this.cacheMap.set(groupName, new Set());

        const watcherSet = this.cacheMap.get(groupName)!;
        const isCacheEmpty = watcherSet.size === 0;
        watcherSet.add(watcherInstance);

        if (isCacheEmpty && this.connectionManager.isConnected)
            this.connectionManager.send('Cache.Subscribe', groupName);
    }

    public unsubscribeWatcher(watcherInstance: ICacheWatcher): void {
        const groupName = watcherInstance.group;
        if (!groupName || !this.cacheMap.has(groupName))
            return;

        const watcherSet = this.cacheMap.get(groupName)!;
        watcherSet.delete(watcherInstance);

        if (watcherSet.size === 0) {
            this.cacheMap.delete(groupName);
            if (this.connectionManager.isConnected)
                this.connectionManager.send('Cache.Unsubscribe', groupName);
        }
    }

    public resubscribeAll(): void {
        if (!this.connectionManager.isConnected)
            return;

        for (const [groupName, watcherSet] of this.cacheMap.entries()) {
            if (watcherSet.size > 0)
                this.connectionManager.send('Cache.Subscribe', groupName);
        }
    }

    public dispatchValueChanged(groupName: string, key: string, value: any): void {
        const watcherSet = this.cacheMap.get(groupName);
        if (!watcherSet)
            return;

        for (const watcherInstance of Array.from(watcherSet))
            watcherInstance.dispatchValueChanged(key, value);
    }
}
