import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { ConfigurationService } from './configuration.service';
import { GeneralConfig } from '../models/general-config.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class GeneralService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly generalConfig = signal<GeneralConfig>({
        startMinimizedToTray: false,
        minimizeToTrayOnClose: true,
        maxMessageDepth: 5,
        maxTopicHistorySize: 20,
    });
    private originalConfig: GeneralConfig | null = null;
    readonly isModified = signal<boolean>(false);
    readonly loading = signal<boolean>(false);

    load(): void {
        this.loading.set(true);
        this.configService.general
            .get()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    if (data) {
                        this.originalConfig = JSON.parse(JSON.stringify(data));
                        this.generalConfig.set(data);
                        this.isModified.set(false);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações gerais.',
                    ),
            });
    }

    updateGeneralConfig(updated: Partial<GeneralConfig>): void {
        this.generalConfig.update((prev) => ({
            ...prev,
            ...updated,
        }));
        this.checkModified();
    }

    private checkModified(): void {
        if (!this.originalConfig) {
            this.isModified.set(false);
            return;
        }
        const currentJson = JSON.stringify(this.generalConfig());
        const originalJson = JSON.stringify(this.originalConfig);
        this.isModified.set(currentJson !== originalJson);
    }


    save(): void {
        const payload = this.generalConfig();
        this.loading.set(true);
        this.configService.general
            .save(payload)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: () => {
                    this.originalConfig = JSON.parse(JSON.stringify(payload));
                    this.isModified.set(false);
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações gerais.',
                    ),
            });
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
