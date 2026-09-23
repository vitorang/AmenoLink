export type ProjectType = 'csharp' | 'dart' | 'python' | 'typescript';

export interface ProjectTypeMetadata {
    label: string;
    manifestFileName: string;
}

export const PROJECT_TYPE_METADATA: Record<ProjectType, ProjectTypeMetadata> = {
    csharp: {
        label: 'C#',
        manifestFileName: '*.csproj',
    },
    dart: {
        label: 'Dart',
        manifestFileName: 'pubspec.yaml',
    },
    python: {
        label: 'Python',
        manifestFileName: 'requirements.txt',
    },
    typescript: {
        label: 'TypeScript',
        manifestFileName: 'package.json',
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
    cSharp: string;
    dart: string;
    python: string;
    typeScript: string;
    isDebugging: boolean;
}
