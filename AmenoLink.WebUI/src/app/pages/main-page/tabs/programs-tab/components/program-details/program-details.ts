import { Component, input, output, inject, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ProgramConfig, ProgramConfigAction } from '../../../../../../models/program-config.model';
import { ConfigurationService } from '../../../../../../services/configuration.service';
import { ProgramsService } from '../../../../../../services/programs.service';
import { ActionEntry } from '../action-entry/action-entry';
import {
    ActionRegisterModal,
    ActionRegisterModalData,
} from '../action-register-modal/action-register-modal';
import { AlertDialogComponent } from '../../../../../../components/alert-dialog/alert-dialog.component';

@Component({
    selector: 'app-program-details',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatDialogModule,
        ActionEntry,
    ],
    templateUrl: './program-details.html',
    styleUrl: './program-details.scss',
})
export class ProgramDetails {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);
    private readonly programsService = inject(ProgramsService);

    readonly program = input.required<ProgramConfig>();
    readonly programChange = output<ProgramConfig>();

    readonly sortedActions = computed<ProgramConfigAction[]>(() =>
        [...(this.program().actions || [])].sort((actionA, actionB) =>
            (actionA.route || '').localeCompare(actionB.route || '', undefined, {
                numeric: true,
                sensitivity: 'base',
            }),
        ),
    );

    get programName(): string {
        return this.getFileName(this.program().path);
    }

    onChangeExecutable(): void {
        this.configService.selectExecutable(this.program().path).subscribe({
            next: (selectedPath) => {
                if (!selectedPath)
                    return;

                this.programChange.emit({
                    ...this.program(),
                    path: selectedPath,
                });
            },
        });
    }

    onAddAction(): void {
        const dialogRef = this.dialog.open<
            ActionRegisterModal,
            ActionRegisterModalData,
            ProgramConfigAction
        >(ActionRegisterModal);

        dialogRef.afterClosed().subscribe((result) => {
            if (!result)
                return;

            for (const program of this.programsService.programs()) {
                const hasRoute = (program.actions || []).some(
                    (action) => action.route === result.route,
                );
                if (hasRoute) {
                    const programName = this.getFileName(program.path);
                    this.showActionExistsDialog(result.route, programName);
                    return;
                }
            }

            const currentActions = this.program().actions || [];
            const updatedActions = [...currentActions, result];

            this.programChange.emit({
                ...this.program(),
                actions: updatedActions,
            });
        });
    }

    onEditAction(targetAction: ProgramConfigAction): void {
        const dialogRef = this.dialog.open<
            ActionRegisterModal,
            ActionRegisterModalData,
            ProgramConfigAction
        >(ActionRegisterModal, {
            data: { action: targetAction },
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (!result)
                return;

            for (const program of this.programsService.programs()) {
                const hasRoute = (program.actions || []).some(
                    (action) => action !== targetAction && action.route === result.route,
                );
                if (hasRoute) {
                    const programName = this.getFileName(program.path);
                    this.showActionExistsDialog(result.route, programName);
                    return;
                }
            }

            const currentActions = this.program().actions || [];
            const updatedActions = currentActions.map((action) => (action === targetAction ? result : action));

            this.programChange.emit({
                ...this.program(),
                actions: updatedActions,
            });
        });
    }

    onRemoveAction(targetAction: ProgramConfigAction): void {
        const updatedActions = (this.program().actions || []).filter((action) => action !== targetAction);
        this.programChange.emit({
            ...this.program(),
            actions: updatedActions,
        });
    }

    onMaxInstancesChange(value: number | null): void {
        this.programChange.emit({
            ...this.program(),
            maxInstances: this.sanitizePositiveInteger(value),
        });
    }

    onStartupTimeoutChange(value: number | null): void {
        this.programChange.emit({
            ...this.program(),
            startupTimeoutInSeconds: this.sanitizePositiveInteger(value),
        });
    }

    onSlidingExpirationChange(value: number | null): void {
        this.programChange.emit({
            ...this.program(),
            slidingExpirationInSeconds: this.sanitizePositiveInteger(value),
        });
    }

    onBlur(event: FocusEvent): void {
        const inputElement = event.target as HTMLInputElement;
        if (inputElement && (!inputElement.value || Number(inputElement.value) < 1))
            inputElement.value = '1';

        this.programChange.emit({ ...this.program() });
    }

    private getFileName(path: string): string {
        if (!path)
            return '';

        const parts = path.split(/[/\\]/);
        return parts[parts.length - 1] || path;
    }

    private showActionExistsDialog(route: string, programName: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: {
                title: 'Conflito de Ações',
                message: `A ação "${route}" está configurada em "${programName}".`,
            },
        });
    }

    private sanitizePositiveInteger(value: number | null): number {
        if (!value || value < 1)
            return 1;

        return Math.floor(value);
    }
}
