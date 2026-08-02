import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProgramConfigHandler } from '../../../../../../models/program-config.model';

export interface HandlerRegisterModalData {
  handler?: ProgramConfigHandler;
}

@Component({
  selector: 'app-handler-register-modal',
  imports: [
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './handler-register-modal.html',
  styleUrl: './handler-register-modal.scss',
})
export class HandlerRegisterModal {
  private readonly dialogRef = inject(MatDialogRef<HandlerRegisterModal>);
  private readonly data: HandlerRegisterModalData = inject(MAT_DIALOG_DATA, { optional: true }) || {};

  readonly isEditing = !!this.data.handler;
  readonly route = signal<string>(this.data.handler?.route || '');
  readonly timeoutInSeconds = signal<number>(this.data.handler?.timeoutInSeconds ?? 10);

  get isValid(): boolean {
    const value = this.route();
    if (!value) return false;
    if (value.trim() !== value) return false;
    if (value.trim().length === 0) return false;
    return true;
  }

  onTimeoutChange(value: number | null): void {
    if (!value || value < 1) {
      this.timeoutInSeconds.set(1);
    } else {
      this.timeoutInSeconds.set(Math.floor(value));
    }
  }

  onTimeoutBlur(event: FocusEvent): void {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement && (!inputElement.value || Number(inputElement.value) < 1)) {
      inputElement.value = '1';
      this.timeoutInSeconds.set(1);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    if (!this.isValid) return;

    const result: ProgramConfigHandler = {
      route: this.route().trim(),
      timeoutInSeconds: this.timeoutInSeconds(),
    };

    this.dialogRef.close(result);
  }
}
