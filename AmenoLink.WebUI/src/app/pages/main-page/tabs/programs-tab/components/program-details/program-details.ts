import { Component, input, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ProgramConfig, ProgramConfigAction } from '../../../../../../models/program-config.model';
import { ConfigurationService } from '../../../../../../services/configuration.service';
import { ActionEntry } from '../action-entry/action-entry';
import {
    ActionRegisterModal,
    ActionRegisterModalData,
} from '../action-register-modal/action-register-modal';

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

    readonly program = input.required<ProgramConfig>();
    readonly programChange = output<ProgramConfig>();

    get programName(): string {
        const path = this.program().path;
        if (!path)
            return '';

        const parts = path.split(/[/\\]/);
        return parts[parts.length - 1] || path;
    }

    onChangeExecutable(): void {
        this.configService.selectExecutable().subscribe({
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

            const currentActions = this.program().actions || [];
            const updatedActions = [...currentActions, result];

            this.programChange.emit({
                ...this.program(),
                actions: updatedActions,
            });
        });
    }

    onEditAction(actionIndex: number): void {
        const targetAction = this.program().actions[actionIndex];
        if (!targetAction)
            return;

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

            const updatedActions = [...this.program().actions];
            updatedActions[actionIndex] = result;

            this.programChange.emit({
                ...this.program(),
                actions: updatedActions,
            });
        });
    }

    onRemoveAction(actionIndex: number): void {
        const updatedActions = this.program().actions.filter((_, i) => i !== actionIndex);
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

    private sanitizePositiveInteger(value: number | null): number {
        if (!value || value < 1)
            return 1;

        return Math.floor(value);
    }
}
