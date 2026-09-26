import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { CacheManager } from './cache_manager';
import { TopicMessage } from './dtos';
import { clientSetup, AmenoException } from './shared';
import { IConnectionManager, ITopic, TopicManager } from './topic_manager';

export enum ConnectionStatus {
    Disconnected = 'Disconnected',
    Connecting = 'Connecting',
    Connected = 'Connected'
}

export class ConnectionManager implements IConnectionManager {
    private connection: HubConnection | null = null;
    public status: ConnectionStatus = ConnectionStatus.Disconnected;
    public readonly topicManager: TopicManager;
    public readonly cacheManager: CacheManager;
    private readonly statusListeners: Set<(status: ConnectionStatus) => void> = new Set();

    constructor() {
        this.topicManager = new TopicManager(this);
        this.cacheManager = new CacheManager(this);
    }

    public get isConnected(): boolean {
        return this.status === ConnectionStatus.Connected;
    }

    public async connect(options?: {
        onStatusChange?: (status: ConnectionStatus) => void;
        maxAttempts?: number;
        timeoutSeconds?: number;
    }): Promise<void> {
        if (options?.onStatusChange)
            this.statusListeners.add(options.onStatusChange);

        if (this.connection)
            return;

        this.updateStatus(ConnectionStatus.Connecting);

        let url = `${clientSetup.originUrl}/app-hub`;
        if (clientSetup.appName) {
            const encodedAppName = encodeURIComponent(clientSetup.appName);
            url = `${url}?appName=${encodedAppName}`;
        }

        const maxAttempts = options?.maxAttempts ?? 5;
        const retryDelays = Array.from({ length: maxAttempts }, () => 2000);

        this.connection = new HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect(retryDelays)
            .configureLogging(LogLevel.None)
            .build();

        this.connection.onreconnecting(() => {
            this.updateStatus(ConnectionStatus.Connecting);
        });

        this.connection.onreconnected(() => {
            this.onConnectionReconnected();
        });

        this.connection.onclose(() => {
            this.onConnectionClosed();
        });

        this.connection.on('Topic.Message', (topicName: string, message: TopicMessage<unknown>) => {
            this.topicManager.dispatchMessage(topicName, message);
        });

        this.connection.on('Cache.ValueChanged', (groupName: string, key: string, value: any) => {
            this.cacheManager.dispatchValueChanged(groupName, key, value);
        });

        const timeoutSeconds = options?.timeoutSeconds ?? 5.0;
        const timeoutPromise = new Promise<never>((_, reject) => {
            setTimeout(() => reject(new Error('Connection timeout')), timeoutSeconds * 1000);
        });

        try {
            await Promise.race([this.connection.start(), timeoutPromise]);
            this.onConnectionOpened();
        } catch (_) {
            this.updateStatus(ConnectionStatus.Disconnected);
        }
    }

    public async disconnect(): Promise<void> {
        const currentConnection = this.connection;
        if (currentConnection) {
            await currentConnection.stop();
            this.connection = null;
        }
    }

    private onConnectionOpened(): void {
        this.updateStatus(ConnectionStatus.Connected);
        this.topicManager.resubscribeAll();
        this.cacheManager.resubscribeAll();
    }

    private onConnectionReconnected(): void {
        this.updateStatus(ConnectionStatus.Connected);
        this.topicManager.resubscribeAll();
        this.cacheManager.resubscribeAll();
    }

    private onConnectionClosed(): void {
        this.updateStatus(ConnectionStatus.Disconnected);
    }

    private updateStatus(newStatus: ConnectionStatus): void {
        this.status = newStatus;
        for (const listener of Array.from(this.statusListeners))
            listener(newStatus);
    }

    public async send(method: string, ...args: any[]): Promise<void> {
        const currentConnection = this.connection;
        if (!currentConnection || !this.isConnected)
            throw new AmenoException(`Não é possível enviar '${method}': cliente desconectado.`);

        await currentConnection.send(method, ...args);
    }

    public subscribeTopic(topicInstance: ITopic): void {
        this.topicManager.subscribeTopic(topicInstance);
    }

    public unsubscribeTopic(topicInstance: ITopic): void {
        this.topicManager.unsubscribeTopic(topicInstance);
    }
}

export const connectionManager = new ConnectionManager();
