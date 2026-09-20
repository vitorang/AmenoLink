import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { ConfigurationService } from './configuration.service';
import { SpaConfig } from '../models/spa-config.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class SpaService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly spaConfigs = signal<SpaConfig[]>([]);
    readonly selectedSpaConfig = signal<SpaConfig | null>(null);
    private originalSpaJson: string = '[]';
    readonly isModified = signal<boolean>(false);
    readonly loading = signal<boolean>(false);

    getSpaUrl(route: string): string {
        return `${this.configService.hostUrl}/spa/${route}/`;
    }

    load(): void {
        if (this.loading())
            return;

        this.loading.set(true);
        this.configService.spa
            .get()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    const list = data ?? [];
                    this.originalSpaJson = JSON.stringify(list);
                    this.spaConfigs.set(list);
                    this.isModified.set(false);

                    const currentSelected = this.selectedSpaConfig();
                    if (currentSelected?.route) {
                        const matched = list.find((c) => c.route === currentSelected.route);
                        this.selectedSpaConfig.set(matched || (list.length > 0 ? list[0] : null));
                    } else {
                        this.selectedSpaConfig.set(list.length > 0 ? list[0] : null);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações de SPA.',
                    ),
            });
    }

    addSpaConfig(route: string): void {
        const key = route.trim();
        if (!key)
            return;

        const exists = this.spaConfigs().some((c) => c.route === key);
        if (exists) {
            this.showErrorDialog('SPA Existente', `Já existe uma configuração de SPA com a rota '${key}'.`);
            return;
        }

        const newConfig: SpaConfig = {
            route: key,
            rootPath: '',
            indexFile: 'index.html',
        };

        this.spaConfigs.update((prev) => [...prev, newConfig]);
        this.selectedSpaConfig.set(newConfig);
        this.checkModified();
    }

    removeSpaConfig(config: SpaConfig): void {
        this.spaConfigs.update((prev) => prev.filter((c) => c !== config));
        if (this.selectedSpaConfig() === config) {
            const remaining = this.spaConfigs();
            this.selectedSpaConfig.set(remaining.length > 0 ? remaining[0] : null);
        }
        this.checkModified();
    }

    selectSpaConfig(config: SpaConfig): void {
        this.selectedSpaConfig.set(config);
    }

    updateSelectedSpaConfig(updated: SpaConfig): void {
        const current = this.selectedSpaConfig();
        if (!current)
            return;

        this.spaConfigs.update((prev) => prev.map((item) => (item === current ? updated : item)));
        this.selectedSpaConfig.set(updated);
        this.checkModified();
    }

    private checkModified(): void {
        const currentJson = JSON.stringify(this.spaConfigs());
        this.isModified.set(currentJson !== this.originalSpaJson);
    }

    save(): void {
        const sortedConfigs = [...this.spaConfigs()].sort((a, b) =>
            a.route.localeCompare(b.route, undefined, {
                numeric: true,
                sensitivity: 'base',
            }),
        );

        this.spaConfigs.set(sortedConfigs);

        this.loading.set(true);
        this.configService.spa
            .save(sortedConfigs)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: () => {
                    this.originalSpaJson = JSON.stringify(sortedConfigs);
                    this.isModified.set(false);
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações de SPA.',
                    ),
            });
    }

    selectFolder(currentPath?: string) {
        return this.configService.spa.selectFolder(currentPath);
    }

    openInBrowser(route: string): void {
        const url = this.getSpaUrl(route);
        this.configService.openUrl(url).subscribe({
            error: (err) =>
                this.showErrorDialog(
                    'Erro ao Abrir URL',
                    err?.message || 'Não foi possível abrir a URL.',
                ),
        });
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
