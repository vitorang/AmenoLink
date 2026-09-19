import { ProjectConfig } from './project-config.model';

export interface GeneralConfig {
    startMinimizedToTray: boolean;
    minimizeToTrayOnClose: boolean;
    maxMessageDepth: number;
    maxTopicHistorySize: number;
    projects: ProjectConfig[];
}
