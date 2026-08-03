import { Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ProgramConfigHandler } from '../../../../../../models/program-config.model';

@Component({
    selector: 'app-handler-entry',
    imports: [MatIconModule, MatButtonModule],
    templateUrl: './handler-entry.html',
    styleUrl: './handler-entry.scss',
})
export class HandlerEntry {
    readonly handler = input.required<ProgramConfigHandler>();
    readonly remove = output<void>();

    onRemoveClick(event: MouseEvent): void {
        event.stopPropagation();
        this.remove.emit();
    }
}
