import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { ConfigurationService } from './configuration.service';
import { GeneralConfig } from '../models/general-config.model';
import { PackageInstallInstructions, PackageVersion, ProjectConfig } from '../models/project-config.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class GeneralService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly generalConfig = signal<GeneralConfig>({
        startMinimizedToTray: false,
        minimizeToTrayOnClose: true,
        maxMessageDepth: 5,
        maxTopicHistorySize: 20,
        projects: [],
    });
    private originalConfig: GeneralConfig | null = null;
    readonly isModified = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly loadingVersions = signal<boolean>(false);
    readonly projectVersions = signal<Record<string, PackageVersion>>({});
    readonly installInstructions = signal<PackageInstallInstructions>({
        dart: '',
        python: '',
        isDebugging: false,
    });

    load(): void {
        if (this.loading())
            return;

        this.loading.set(true);
        this.loadInstallInstructions();
        this.configService.general
            .get()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    if (data) {
                        this.originalConfig = JSON.parse(JSON.stringify(data));
                        this.generalConfig.set(data);
                        this.isModified.set(false);
                        this.cleanRemovedProjectVersions(data.projects || []);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações gerais.',
                    ),
            });
    }

    private cleanRemovedProjectVersions(projects: ProjectConfig[]): void {
        const currentManifestPaths = new Set(projects.map((project) => project.packageManifest.toLowerCase()));
        this.projectVersions.update((previousMap) => {
            const nextMap: Record<string, PackageVersion> = {};
            for (const [path, version] of Object.entries(previousMap)) {
                if (currentManifestPaths.has(path.toLowerCase()))
                    nextMap[path] = version;
            }
            return nextMap;
        });
    }

    updateGeneralConfig(updated: Partial<GeneralConfig>): void {
        this.generalConfig.update((prev) => ({
            ...prev,
            ...updated,
        }));
        this.checkModified();
    }

    addProject(project: ProjectConfig): void {
        const currentProjects = this.generalConfig().projects || [];
        this.updateGeneralConfig({
            projects: [...currentProjects, project],
        });
    }

    removeProject(index: number): void {
        const currentProjects = this.generalConfig().projects || [];
        const nextProjects = currentProjects.filter((_, i) => i !== index);
        this.updateGeneralConfig({
            projects: nextProjects,
        });
        this.cleanRemovedProjectVersions(nextProjects);
    }

    private checkModified(): void {
        if (!this.originalConfig) {
            this.isModified.set(false);
            return;
        }
        const currentJson = JSON.stringify(this.generalConfig());
        const originalJson = JSON.stringify(this.originalConfig);
        this.isModified.set(currentJson !== originalJson);
    }


    save(): void {
        const payload = this.generalConfig();
        this.loading.set(true);
        this.configService.general
            .save(payload)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: () => {
                    this.originalConfig = JSON.parse(JSON.stringify(payload));
                    this.isModified.set(false);
                    this.cleanRemovedProjectVersions(payload.projects || []);
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações gerais.',
                    ),
            });
    }

    checkProjectVersions(): void {
        if (this.loadingVersions())
            return;

        const projects = this.generalConfig().projects || [];
        if (projects.length === 0)
            return;

        this.loadingVersions.set(true);

        const requests = projects.map((project) =>
            this.configService.general
                .getPackageVersion(project.packageManifest, project.type)
                .pipe(
                    catchError((err) =>
                        of<PackageVersion>({
                            manifestPath: project.packageManifest,
                            version: '',
                            appVersion: '',
                            isCompatible: false,
                            errorReason: err?.message || 'Erro de comunicação ao verificar versão',
                        }),
                    ),
                ),
        );

        forkJoin(requests)
            .pipe(finalize(() => this.loadingVersions.set(false)))
            .subscribe({
                next: (results) => {
                    const map: Record<string, PackageVersion> = {};
                    for (const result of results)
                        map[result.manifestPath.toLowerCase()] = result;

                    this.projectVersions.set(map);
                },
            });
    }

    loadInstallInstructions(): void {
        this.configService.general.getPackageInstallInstructions().subscribe({
            next: (instructions) => {
                if (instructions)
                    this.installInstructions.set(instructions);
            },
        });
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
