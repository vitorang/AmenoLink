import { TopicMessage } from './dtos';

export interface IConnectionManager {
    readonly isConnected: boolean;
    send(method: string, ...args: any[]): Promise<void>;
    subscribeTopic(topicInstance: ITopic): void;
    unsubscribeTopic(topicInstance: ITopic): void;
}

export interface ITopic {
    readonly name: string;
    dispatchMessage(message: TopicMessage<unknown>): void;
}

export class TopicManager {
    private readonly connectionManager: IConnectionManager;
    public readonly topicMap: Map<string, Set<ITopic>> = new Map();

    constructor(connectionManager: IConnectionManager) {
        this.connectionManager = connectionManager;
    }

    public subscribeTopic(topicInstance: ITopic): void {
        const topicName = topicInstance.name;
        if (!topicName)
            return;

        if (!this.topicMap.has(topicName))
            this.topicMap.set(topicName, new Set());

        const topicSet = this.topicMap.get(topicName)!;
        const isTopicEmpty = topicSet.size === 0;
        topicSet.add(topicInstance);

        if (isTopicEmpty && this.connectionManager.isConnected)
            this.connectionManager.send('Topic.Subscribe', topicName);
    }

    public unsubscribeTopic(topicInstance: ITopic): void {
        const topicName = topicInstance.name;
        if (!topicName || !this.topicMap.has(topicName))
            return;

        const topicSet = this.topicMap.get(topicName)!;
        topicSet.delete(topicInstance);

        if (topicSet.size === 0) {
            this.topicMap.delete(topicName);
            if (this.connectionManager.isConnected)
                this.connectionManager.send('Topic.Unsubscribe', topicName);
        }
    }

    public resubscribeAll(): void {
        if (!this.connectionManager.isConnected)
            return;

        for (const [topicName, topicSet] of this.topicMap.entries()) {
            if (topicSet.size > 0)
                this.connectionManager.send('Topic.Subscribe', topicName);
        }
    }

    public dispatchMessage(topicName: string, topicMessage: TopicMessage<unknown>): void {
        const topicSet = this.topicMap.get(topicName);
        if (!topicSet)
            return;

        for (const topicInstance of Array.from(topicSet))
            topicInstance.dispatchMessage(topicMessage);
    }
}
