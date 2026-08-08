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
        maxMessageDepth: 5,
        maxTopicHistorySize: 20,
    });
    readonly loading = signal<boolean>(false);

    load(): void {
        this.loading.set(true);
        this.configService
            .getGeneralConfig()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    if (data)
                        this.generalConfig.set(data);
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
    }

    save(): void {
        this.loading.set(true);
        this.configService
            .saveGeneralConfig(this.generalConfig())
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
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
