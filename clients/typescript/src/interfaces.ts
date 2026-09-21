import { Message, TopicMessage, ActionRequest } from './dtos';

export type ActionHandler<T, R> = (input: T) => Promise<R> | R;
export type CacheAllHandler = (key: string, value: any) => void;
export type CacheKeyHandler<T> = (value: T | null) => void;
export type TopicHandler<T> = (message: TopicMessage<T>) => void;

export interface Action {
    readonly name: string;
    request<T>(payload?: unknown): Promise<T>;
    queue(payload?: unknown): Promise<void>;
}

export interface CacheWatcher {
    readonly group: string;
    all(handler: CacheAllHandler): void;
    key<T>(key: string, handler: CacheKeyHandler<T>): void;
    dispose(): void;
}

export interface Cache {
    readonly group: string;
    watch(): CacheWatcher;
    get<T>(key: string): Promise<T | null>;
    set<T>(key: string, value: T): Promise<void>;
    getOrCreate<T>(key: string, creator: () => Promise<T> | T): Promise<T>;
    all(): Promise<Record<string, any>>;
    clear(): Promise<void>;
    delete(key: string): Promise<void>;
}

export interface Topic<T> {
    readonly name: string;
    subscribe(handler: TopicHandler<T>): void;
    publish(value: T, options?: { previous?: Message | null }): Promise<void>;
    dispose(): void;
}

export interface ActionContext {
    readonly request: ActionRequest<unknown>;
    log(message: string): void;
}

export interface ActionRouter {
    add<T, R>(route: string, handler: ActionHandler<T, R>): void;
    serve(): void;
}
