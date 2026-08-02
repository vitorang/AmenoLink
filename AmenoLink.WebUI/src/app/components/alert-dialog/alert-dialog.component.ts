import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface AlertDialogData {
  title: string;
  message: string;
}

@Component({
  selector: 'app-alert-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close color="primary">OK</button>
    </mat-dialog-actions>
  `,
})
export class AlertDialogComponent {
  readonly data = inject<AlertDialogData>(MAT_DIALOG_DATA);
}
