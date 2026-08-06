export interface ProgramConfigAction {
    route: string;
    timeoutInSeconds: number;
}

export interface ProgramConfig {
    id: string;
    path: string;
    actions: ProgramConfigAction[];
    slidingExpirationInSeconds: number;
    startupTimeoutInSeconds: number;
    maxInstances: number;
}
