import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { ConfigurationService } from './configuration.service';
import { CacheConfig } from '../models/cache-config.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class CacheService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly cacheConfigs = signal<CacheConfig[]>([]);
    readonly selectedCacheConfig = signal<CacheConfig | null>(null);
    readonly loading = signal<boolean>(false);

    load(): void {
        this.loading.set(true);
        this.configService
            .getCacheConfigs()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    const list = data ?? [];
                    this.cacheConfigs.set(list);

                    const currentSelected = this.selectedCacheConfig();
                    if (currentSelected?.groupKey) {
                        const matched = list.find((c) => c.groupKey === currentSelected.groupKey);
                        this.selectedCacheConfig.set(matched || (list.length > 0 ? list[0] : null));
                    } else {
                        this.selectedCacheConfig.set(list.length > 0 ? list[0] : null);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações de cache.',
                    ),
            });
    }

    addCacheConfig(groupKey: string): void {
        const key = groupKey.trim();
        if (!key)
            return;

        const exists = this.cacheConfigs().some((c) => c.groupKey === key);
        if (exists) {
            this.showErrorDialog('Grupo Existente', `O grupo de cache '${key}' já existe.`);
            return;
        }

        const newConfig: CacheConfig = {
            groupKey: key,
            inactivityExpirationInSeconds: 300,
            totalExpirationInSeconds: 3600,
        };

        this.cacheConfigs.update((prev) => [...prev, newConfig]);
        this.selectedCacheConfig.set(newConfig);
    }

    removeCacheConfig(config: CacheConfig): void {
        this.cacheConfigs.update((prev) => prev.filter((c) => c !== config));
        if (this.selectedCacheConfig() === config) {
            const remaining = this.cacheConfigs();
            this.selectedCacheConfig.set(remaining.length > 0 ? remaining[0] : null);
        }
    }

    selectCacheConfig(config: CacheConfig): void {
        this.selectedCacheConfig.set(config);
    }

    renameCacheConfig(oldGroupKey: string, newGroupKey: string): void {
        const newKey = newGroupKey.trim();
        if (!newKey || oldGroupKey === newKey)
            return;

        const exists = this.cacheConfigs().some((c) => c.groupKey === newKey);
        if (exists) {
            this.showErrorDialog('Grupo Existente', `O grupo de cache '${newKey}' já existe.`);
            return;
        }

        this.cacheConfigs.update((prev) =>
            prev.map((c) => (c.groupKey === oldGroupKey ? { ...c, groupKey: newKey } : c)),
        );

        const currentSelected = this.selectedCacheConfig();
        if (currentSelected?.groupKey === oldGroupKey) {
            this.selectedCacheConfig.set({
                ...currentSelected,
                groupKey: newKey,
            });
        }
    }

    updateSelectedCacheConfig(updated: CacheConfig): void {
        const current = this.selectedCacheConfig();
        if (!current)
            return;

        this.cacheConfigs.update((prev) => prev.map((item) => (item === current ? updated : item)));
        this.selectedCacheConfig.set(updated);
    }

    save(): void {
        const sortedConfigs = [...this.cacheConfigs()].sort((a, b) =>
            a.groupKey.localeCompare(b.groupKey, undefined, {
                numeric: true,
                sensitivity: 'base',
            }),
        );

        this.cacheConfigs.set(sortedConfigs);

        this.loading.set(true);
        this.configService
            .saveCacheConfigs(sortedConfigs)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações de cache.',
                    ),
            });
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
