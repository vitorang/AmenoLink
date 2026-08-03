import { Component, input, output, inject, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { CacheConfig } from '../../../../../../models/cache-config.model';
import { CacheDataService, CacheEntryItem } from '../../../../../../services/cache-data.service';

@Component({
    selector: 'app-cache-details',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatTableModule,
        MatProgressSpinnerModule,
    ],
    templateUrl: './cache-details.html',
    styleUrl: './cache-details.scss',
})
export class CacheDetails {
    private readonly cacheDataService = inject(CacheDataService);

    readonly config = input.required<CacheConfig>();
    readonly configChange = output<CacheConfig>();
    readonly rename = output<void>();

    readonly cacheEntries = signal<CacheEntryItem[] | null>(null);
    readonly loadingEntries = signal<boolean>(false);
    readonly displayedColumns: string[] = ['key', 'value', 'actions'];

    private currentGroupKey: string | null = null;

    constructor() {
        effect(() => {
            const groupKey = this.config().groupKey;
            if (this.currentGroupKey !== groupKey) {
                this.currentGroupKey = groupKey;
                this.cacheEntries.set(null);
            }
        });
    }

    onRenameGroup(): void {
        this.rename.emit();
    }

    onRefreshValues(): void {
        const groupKey = this.config().groupKey;
        this.loadingEntries.set(true);

        this.cacheDataService
            .getAllEntries(groupKey)
            .pipe(
                catchError(() => of({})),
                finalize(() => this.loadingEntries.set(false)),
            )
            .subscribe({
                next: (data: Record<string, unknown>) => {
                    const record = data || {};
                    const keys = Object.keys(record);
                    if (keys.length === 0) {
                        this.cacheEntries.set([]);
                        return;
                    }

                    const sortedKeys = keys.sort((a, b) =>
                        a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' }),
                    );

                    const entries: CacheEntryItem[] = sortedKeys.map((key) => ({
                        key,
                        value: record[key] ?? null,
                    }));

                    this.cacheEntries.set(entries);
                },
            });
    }

    onDeleteEntry(key: string): void {
        const groupKey = this.config().groupKey;
        this.cacheDataService.deleteEntry(groupKey, key).subscribe({
            next: () => this.onRefreshValues(),
        });
    }

    formatValue(value: unknown): string {
        if (value === null || value === undefined)
            return 'null';
        if (typeof value === 'object')
            return JSON.stringify(value);

        return String(value);
    }

    onInactivityExpirationChange(value: number | null): void {
        this.configChange.emit({
            ...this.config(),
            inactivityExpirationInSeconds: this.sanitizeNonNegativeInteger(value),
        });
    }

    onTotalExpirationChange(value: number | null): void {
        this.configChange.emit({
            ...this.config(),
            totalExpirationInSeconds: this.sanitizeNonNegativeInteger(value),
        });
    }

    onBlur(event: FocusEvent): void {
        const inputElement = event.target as HTMLInputElement;
        if (inputElement && (!inputElement.value || Number(inputElement.value) < 0))
            inputElement.value = '0';

        this.configChange.emit({ ...this.config() });
    }

    private sanitizeNonNegativeInteger(value: number | null): number {
        if (value === null || value === undefined || value < 0)
            return 0;

        return Math.floor(value);
    }
}
