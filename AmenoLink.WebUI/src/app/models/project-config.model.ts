export type ProjectType = 'python' | 'dart';

export interface ProjectTypeMetadata {
    label: string;
    manifestFileName: string;
    cssClass: string;
}

export const PROJECT_TYPE_METADATA: Record<ProjectType, ProjectTypeMetadata> = {
    dart: {
        label: 'Dart',
        manifestFileName: 'pubspec.yaml',
        cssClass: 'type-dart',
    },
    python: {
        label: 'Python',
        manifestFileName: 'requirements.txt',
        cssClass: 'type-python',
    },
};

export interface ProjectConfig {
    name: string;
    type: ProjectType;
    packageManifest: string;
}

export interface PackageVersion {
    manifestPath: string;
    version: string;
    appVersion: string;
    isCompatible: boolean;
    errorReason: string | null;
}
