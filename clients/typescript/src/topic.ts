import { ulid } from 'ulid';
import { connectionManager } from './connection_manager';
import { Message, TopicMessage } from './dtos';
import { resourceManager } from './resource_manager';
import { clientSetup, postJson, AmenoException } from './shared';
import { ITopic } from './topic_manager';

export type TopicHandler<T> = (message: TopicMessage<T>) => void;

export class Topic<T> implements ITopic {
    public readonly name: string;
    private disposed = false;
    private readonly handlers: Set<TopicHandler<T>> = new Set();

    constructor(name: string) {
        this.name = name;
    }

    public subscribe(handler: TopicHandler<T>): void {
        this.ensureNotDisposed();
        this.handlers.add(handler);
        connectionManager.topicManager.subscribeTopic(this);
    }

    public async publish(value: T, options?: { previous?: Message | null }): Promise<void> {
        this.ensureNotDisposed();

        const topicMessage: TopicMessage<T> = {
            id: ulid(),
            previous: options?.previous ?? null,
            type: 'TopicMessage',
            createdAt: new Date().toISOString(),
            topic: this.name,
            payload: value,
            appName: clientSetup.appName
        };

        const url = `${clientSetup.originUrl}/api/topic/publish`;
        await postJson(url, topicMessage);
    }

    public dispose(): void {
        this.ensureNotDisposed();
        this.disposed = true;
        this.handlers.clear();
        connectionManager.topicManager.unsubscribeTopic(this);
    }

    public dispatchMessage(message: TopicMessage<unknown>): void {
        if (this.disposed)
            return;

        const typedMessage = message as TopicMessage<T>;
        for (const handler of Array.from(this.handlers))
            handler(typedMessage);
    }

    private ensureNotDisposed(): void {
        if (this.disposed)
            throw new AmenoException(`O tópico '${this.name}' já foi descartado (disposed).`);
    }
}

export function topic<T>(name: string): Topic<T> {
    resourceManager.topics.add(name);
    return new Topic<T>(name);
}
