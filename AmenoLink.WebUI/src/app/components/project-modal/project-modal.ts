import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConfigurationService } from '../../services/configuration.service';
import { ProjectConfig, ProjectType, PROJECT_TYPE_METADATA } from '../../models/project-config.model';
import { FilePickerCard } from '../file-picker-card/file-picker-card';

export interface ProjectModalData {
    title?: string;
    project?: ProjectConfig;
}

@Component({
    selector: 'app-project-modal',
    imports: [
        FormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatButtonModule,
        MatIconModule,
        MatTooltipModule,
        FilePickerCard,
    ],
    templateUrl: './project-modal.html',
    styleUrl: './project-modal.scss',
})
export class ProjectModal {
    private static lastSelectedType: ProjectType = 'dart';

    private readonly dialogRef = inject(MatDialogRef<ProjectModal>);
    private readonly configService = inject(ConfigurationService);
    readonly data: ProjectModalData | null = inject(MAT_DIALOG_DATA, { optional: true });

    readonly title = this.data?.title || 'Adicionar Projeto';
    readonly name = signal<string>(this.data?.project?.name || '');
    readonly type = signal<ProjectType>(this.data?.project?.type || ProjectModal.lastSelectedType);
    readonly packageManifest = signal<string>(this.data?.project?.packageManifest || '');
    readonly selectingFile = signal<boolean>(false);

    readonly expectedFileName = computed<string>(() => {
        const metadata = PROJECT_TYPE_METADATA[this.type()];
        if (!metadata)
            throw new Error(`Tipo de projeto não suportado: ${this.type()}`);

        return metadata.manifestFileName;
    });

    get isValid(): boolean {
        const trimmedName = this.name().trim();
        const trimmedManifest = this.packageManifest().trim();
        if (trimmedName.length === 0)
            return false;
        if (trimmedManifest.length === 0)
            return false;

        return true;
    }

    onSelectManifest(): void {
        if (this.selectingFile())
            return;

        this.selectingFile.set(true);
        const currentType = this.type();
        const currentManifest = this.packageManifest();

        this.configService.general
            .selectPackageManifest(currentType, currentManifest || undefined)
            .subscribe({
                next: (selectedPath) => {
                    this.selectingFile.set(false);
                    if (selectedPath)
                        this.packageManifest.set(selectedPath);
                },
                error: () => {
                    this.selectingFile.set(false);
                },
            });
    }

    onTypeChange(newType: ProjectType): void {
        this.type.set(newType);
        this.packageManifest.set('');
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onConfirm(): void {
        if (!this.isValid)
            return;

        ProjectModal.lastSelectedType = this.type();

        const result: ProjectConfig = {
            name: this.name().trim(),
            type: this.type(),
            packageManifest: this.packageManifest().trim(),
        };

        this.dialogRef.close(result);
    }
}
