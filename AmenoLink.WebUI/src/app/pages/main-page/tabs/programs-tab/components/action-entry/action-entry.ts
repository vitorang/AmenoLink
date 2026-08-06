import { Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ProgramConfigAction } from '../../../../../../models/program-config.model';

@Component({
    selector: 'app-action-entry',
    imports: [MatIconModule, MatButtonModule],
    templateUrl: './action-entry.html',
    styleUrl: './action-entry.scss',
})
export class ActionEntry {
    readonly action = input.required<ProgramConfigAction>();
    readonly remove = output<void>();

    onRemoveClick(event: MouseEvent): void {
        event.stopPropagation();
        this.remove.emit();
    }
}
