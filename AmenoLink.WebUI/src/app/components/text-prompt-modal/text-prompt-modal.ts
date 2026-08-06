import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface TextPromptModalData {
    title: string;
    label?: string;
    value?: string;
    icon?: string;
    confirmButtonText?: string;
}

@Component({
    selector: 'app-text-prompt-modal',
    imports: [
        FormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
    ],
    templateUrl: './text-prompt-modal.html',
    styleUrl: './text-prompt-modal.scss',
})
export class TextPromptModal {
    private readonly dialogRef = inject(MatDialogRef<TextPromptModal>);
    readonly data: TextPromptModalData = inject(MAT_DIALOG_DATA);

    readonly title = this.data.title;
    readonly label = this.data.label || 'Nome';
    readonly icon = this.data.icon || '';
    readonly confirmButtonText = this.data.confirmButtonText || 'Salvar';

    readonly value = signal<string>(this.data.value || '');

    get isValid(): boolean {
        const val = this.value();
        if (!val)
            return false;
        if (val.trim() !== val)
            return false;
        if (val.trim().length === 0)
            return false;

        return true;
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onConfirm(): void {
        if (!this.isValid)
            return;

        this.dialogRef.close(this.value().trim());
    }
}
