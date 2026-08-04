export interface ActionError {
    type: string;
    message: string;
}

export interface ActionResponse {
    id: string;
    success: boolean;
    logs: string[];
    result?: unknown;
    error?: ActionError | null;
}
