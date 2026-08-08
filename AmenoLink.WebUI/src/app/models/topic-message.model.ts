export interface TopicMessage {
    id: string;
    topic: string;
    payload?: any;
    previous?: TopicMessage | null;
    type?: string;
    createdAt?: string;
    appName?: string;
}
