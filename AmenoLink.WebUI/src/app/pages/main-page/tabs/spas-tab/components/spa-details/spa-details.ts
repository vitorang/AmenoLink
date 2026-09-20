import { Component, input, output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SpaConfig } from '../../../../../../models/spa-config.model';
import { SpaService } from '../../../../../../services/spa.service';

@Component({
    selector: 'app-spa-details',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule,
    ],
    templateUrl: './spa-details.html',
    styleUrl: './spa-details.scss',
})
export class SpaDetails {
    private readonly spaService = inject(SpaService);

    readonly config = input.required<SpaConfig>();
    readonly configChange = output<SpaConfig>();

    readonly selectingFolder = signal<boolean>(false);

    onRootPathChange(value: string): void {
        this.configChange.emit({
            ...this.config(),
            rootPath: value,
        });
    }

    onIndexFileChange(value: string): void {
        this.configChange.emit({
            ...this.config(),
            indexFile: value,
        });
    }

    onBrowseFolder(): void {
        if (this.selectingFolder())
            return;

        this.selectingFolder.set(true);
        const currentPath = this.config().rootPath;

        this.spaService
            .selectFolder(currentPath || undefined)
            .subscribe({
                next: (selected) => {
                    this.selectingFolder.set(false);
                    if (selected)
                        this.onRootPathChange(selected);
                },
                error: () => {
                    this.selectingFolder.set(false);
                },
            });
    }

    onOpenInBrowser(): void {
        this.spaService.openInBrowser(this.config().route);
    }
}
