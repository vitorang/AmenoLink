import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { ProgramConfig } from '../../../../../../models/program-config.model';

@Component({
    selector: 'app-program-entry',
    imports: [MatIconModule],
    templateUrl: './program-entry.html',
    styleUrl: './program-entry.scss',
})
export class ProgramEntry {
    readonly program = input.required<ProgramConfig>();
    readonly isSelected = input<boolean>(false);

    get programName(): string {
        const path = this.program().path;
        const parts = path.split(/[/\\]/);
        return parts[parts.length - 1] || path;
    }
}
