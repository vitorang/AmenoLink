export type ProjectType = 'python' | 'dart' | 'typescript';

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
    typescript: {
        label: 'TypeScript',
        manifestFileName: 'package.json',
        cssClass: 'type-typescript',
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

export interface PackageInstallInstructions {
    dart: string;
    python: string;
    typeScript: string;
    isDebugging: boolean;
}
