export type ProjectType = 'python' | 'dart';

export interface ProjectConfig {
    name: string;
    type: ProjectType;
    packageManifest: string;
}
