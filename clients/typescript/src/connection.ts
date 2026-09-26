import { ConnectionManager, connectionManager, ConnectionStatus } from './connection_manager';
import { Connection as IConnection } from './interfaces';

export class Connection implements IConnection {
    private static readonly MAX_RECONNECT_ATTEMPTS = 5;
    private static readonly CONNECTION_TIMEOUT_SECONDS = 5.0;

    private readonly listeners: Set<(status: ConnectionStatus) => void> = new Set();
    private readonly manager: ConnectionManager;

    constructor(manager: ConnectionManager = connectionManager) {
        this.manager = manager;
    }

    public subscribe(listener: (status: ConnectionStatus) => void): void {
        this.listeners.add(listener);
    }

    public unsubscribe(listener: (status: ConnectionStatus) => void): void {
        this.listeners.delete(listener);
    }

    public unsubscribeAll(): void {
        this.listeners.clear();
    }

    private onConnectionChanged = (status: ConnectionStatus): void => {
        for (const listener of Array.from(this.listeners))
            listener(status);
    };

    public connect(): Promise<void> {
        return this.manager.connect({
            onStatusChange: this.onConnectionChanged,
            maxAttempts: Connection.MAX_RECONNECT_ATTEMPTS,
            timeoutSeconds: Connection.CONNECTION_TIMEOUT_SECONDS
        });
    }

    public disconnect(): Promise<void> {
        return this.manager.disconnect();
    }
}

export const connection = new Connection();
