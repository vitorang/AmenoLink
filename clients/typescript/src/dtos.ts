export interface Message {
    id: string;
    previous?: Message | null;
    type: string;
    createdAt: string;
    appName?: string;
}

export interface ActionRequest<T> extends Message {
    route: string;
    payload?: T;
}

export interface ActionError {
    type: string;
    message: string;
}

export interface ActionResponse<T> extends Message {
    success: boolean;
    logs: string[];
    result?: T;
    error?: ActionError | null;
}

export interface TopicMessage<T> extends Message {
    topic: string;
    payload?: T;
}

export interface Resources {
    actions: string[];
    caches: string[];
    topics: string[];
    version: string;
}

export interface CacheItem<T> {
    group: string;
    key: string;
    value?: T;
}

export interface CacheEvent<T> {
    group: string;
    key: string;
    value?: T;
}
