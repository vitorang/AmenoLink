import { Component, input, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ProgramConfig, ProgramConfigHandler } from '../../../../../../models/program-config.model';
import { ConfigurationService } from '../../../../../../services/configuration.service';
import { HandlerEntry } from '../handler-entry/handler-entry';
import {
    HandlerRegisterModal,
    HandlerRegisterModalData,
} from '../handler-register-modal/handler-register-modal';

@Component({
    selector: 'app-program-details',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatDialogModule,
        HandlerEntry,
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

    onAddHandler(): void {
        const dialogRef = this.dialog.open<
            HandlerRegisterModal,
            HandlerRegisterModalData,
            ProgramConfigHandler
        >(HandlerRegisterModal);

        dialogRef.afterClosed().subscribe((result) => {
            if (!result)
                return;

            const currentHandlers = this.program().handlers || [];
            const updatedHandlers = [...currentHandlers, result];

            this.programChange.emit({
                ...this.program(),
                handlers: updatedHandlers,
            });
        });
    }

    onEditHandler(handlerIndex: number): void {
        const targetHandler = this.program().handlers[handlerIndex];
        if (!targetHandler)
            return;

        const dialogRef = this.dialog.open<
            HandlerRegisterModal,
            HandlerRegisterModalData,
            ProgramConfigHandler
        >(HandlerRegisterModal, {
            data: { handler: targetHandler },
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (!result)
                return;

            const updatedHandlers = [...this.program().handlers];
            updatedHandlers[handlerIndex] = result;

            this.programChange.emit({
                ...this.program(),
                handlers: updatedHandlers,
            });
        });
    }

    onRemoveHandler(handlerIndex: number): void {
        const updatedHandlers = this.program().handlers.filter((_, i) => i !== handlerIndex);
        this.programChange.emit({
            ...this.program(),
            handlers: updatedHandlers,
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
