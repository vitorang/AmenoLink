import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { GeneralService } from '../../../../services/general.service';
import { handleInputBlur, sanitizeInteger } from '../../../../utils/number.utils';
import { ProjectModal, ProjectModalData } from '../../../../components/project-modal/project-modal';
import { ProjectConfig } from '../../../../models/project-config.model';
import { ProjectEntry } from './components/project-entry/project-entry';

import { AlertDialogComponent } from '../../../../components/alert-dialog/alert-dialog.component';

@Component({
    selector: 'app-general-tab',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSlideToggleModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule,
        ProjectEntry,
    ],
    templateUrl: './general-tab.html',
    styleUrl: './general-tab.scss',
})
export class GeneralTab {
    protected readonly generalService = inject(GeneralService);
    private readonly dialog = inject(MatDialog);

    onAddProject(): void {
        const dialogRef = this.dialog.open<ProjectModal, ProjectModalData, ProjectConfig>(ProjectModal, {
            width: '450px',
        });
        dialogRef.afterClosed().subscribe((result) => {
            if (!result)
                return;

            const existingProjects = this.generalService.generalConfig().projects || [];
            const duplicateProject = existingProjects.find(
                (project) => project.packageManifest.toLowerCase() === result.packageManifest.toLowerCase(),
            );
            if (duplicateProject) {
                this.showProjectExistsDialog(duplicateProject.name);
                return;
            }

            this.generalService.addProject(result);
        });
    }

    private showProjectExistsDialog(projectName: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: {
                title: 'Conflito de Projetos',
                message: `O manifesto já está cadastrado no projeto "${projectName}".`,
            },
        });
    }

    onRemoveProject(index: number): void {
        this.generalService.removeProject(index);
    }

    onCheckVersions(): void {
        if (this.generalService.isModified()) {
            this.dialog.open(AlertDialogComponent, {
                data: {
                    title: 'Alterações Pendentes',
                    message: 'Salve as alterações antes de verificar as versões dos projetos.',
                },
            });
            return;
        }

        this.generalService.checkProjectVersions();
    }

    onToggleStartMinimizedToTray(): void {
        const currentValue = this.generalService.generalConfig().startMinimizedToTray;
        this.generalService.updateGeneralConfig({ startMinimizedToTray: !currentValue });
    }

    onStartMinimizedToTrayChange(startMinimizedToTray: boolean): void {
        this.generalService.updateGeneralConfig({ startMinimizedToTray });
    }

    onToggleMinimizeToTrayOnClose(): void {
        const currentValue = this.generalService.generalConfig().minimizeToTrayOnClose;
        this.generalService.updateGeneralConfig({ minimizeToTrayOnClose: !currentValue });
    }

    onMinimizeToTrayOnCloseChange(minimizeToTrayOnClose: boolean): void {
        this.generalService.updateGeneralConfig({ minimizeToTrayOnClose });
    }


    onMaxMessageDepthChange(value: number | null): void {
        const sanitizedValue = sanitizeInteger(value, 1);
        this.generalService.updateGeneralConfig({ maxMessageDepth: sanitizedValue });
    }

    onMaxTopicHistorySizeChange(value: number | null): void {
        const sanitizedValue = sanitizeInteger(value, 1);
        this.generalService.updateGeneralConfig({ maxTopicHistorySize: sanitizedValue });
    }

    onMaxMessageDepthBlur(event: FocusEvent): void {
        const sanitizedValue = handleInputBlur(event, 1);
        this.generalService.updateGeneralConfig({ maxMessageDepth: sanitizedValue });
    }

    onMaxTopicHistorySizeBlur(event: FocusEvent): void {
        const sanitizedValue = handleInputBlur(event, 1);
        this.generalService.updateGeneralConfig({ maxTopicHistorySize: sanitizedValue });
    }
}
