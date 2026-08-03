import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface CacheGroupRegisterModalData {
    groupKey?: string;
}

@Component({
    selector: 'app-cache-group-register-modal',
    imports: [
        FormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
    ],
    templateUrl: './cache-group-register-modal.html',
    styleUrl: './cache-group-register-modal.scss',
})
export class CacheGroupRegisterModal {
    private readonly dialogRef = inject(MatDialogRef<CacheGroupRegisterModal>);
    private readonly data: CacheGroupRegisterModalData =
        inject(MAT_DIALOG_DATA, { optional: true }) || {};

    readonly isEditing = !!this.data.groupKey;
    readonly groupKey = signal<string>(this.data.groupKey || '');

    get isValid(): boolean {
        const value = this.groupKey();
        if (!value)
            return false;
        if (value.trim() !== value)
            return false;
        if (value.trim().length === 0)
            return false;

        return true;
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onConfirm(): void {
        if (!this.isValid)
            return;

        this.dialogRef.close(this.groupKey());
    }
}
