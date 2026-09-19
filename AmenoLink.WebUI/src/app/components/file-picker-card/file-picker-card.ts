import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
    selector: 'app-file-picker-card',
    imports: [MatIconModule, MatButtonModule, MatTooltipModule],
    templateUrl: './file-picker-card.html',
    styleUrl: './file-picker-card.scss',
})
export class FilePickerCard {
    readonly filePath = input<string>('');
    readonly expectedFileName = input<string>('');
    readonly selecting = input<boolean>(false);
    readonly selectFile = output<void>();

    readonly fileName = computed<string>(() => {
        const path = this.filePath();
        if (!path)
            return '';

        const normalized = path.replace(/\\/g, '/');
        const segments = normalized.split('/');
        return segments[segments.length - 1] || '';
    });

    onSelect(): void {
        if (!this.selecting())
            this.selectFile.emit();
    }
}
