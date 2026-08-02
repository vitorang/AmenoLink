export interface ProgramConfigHandler {
  route: string;
  timeoutInSeconds: number;
}

export interface ProgramConfig {
  id: string;
  path: string;
  handlers: ProgramConfigHandler[];
  slidingExpirationInSeconds: number;
  startupTimeoutInSeconds: number;
  maxInstances: number;
}
