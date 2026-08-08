import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { ulid } from 'ulid';
import { ConfigurationService } from './configuration.service';
import { ProgramConfig } from '../models/program-config.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class ProgramsService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly programs = signal<ProgramConfig[]>([]);
    readonly selectedProgram = signal<ProgramConfig | null>(null);
    readonly loading = signal<boolean>(false);

    load(): void {
        this.loading.set(true);
        this.configService
            .getProgramConfigs()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    const list = data ?? [];
                    this.programs.set(list);

                    const currentSelected = this.selectedProgram();
                    if (currentSelected?.id) {
                        const matched = list.find((p) => p.id === currentSelected.id);
                        this.selectedProgram.set(matched || (list.length > 0 ? list[0] : null));
                    } else {
                        this.selectedProgram.set(list.length > 0 ? list[0] : null);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações dos programas.',
                    ),
            });
    }

    addProgram(): void {
        this.loading.set(true);
        this.configService
            .selectExecutable()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (selectedPath) => {
                    if (!selectedPath)
                        return;

                    const newProgram: ProgramConfig = {
                        id: ulid(),
                        path: selectedPath,
                        actions: [],
                        slidingExpirationInSeconds: 300,
                        startupTimeoutInSeconds: 30,
                        maxInstances: 1,
                    };

                    this.programs.update((prev) => [...prev, newProgram]);
                    this.selectedProgram.set(newProgram);
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Selecionar Arquivo',
                        err?.message || 'Não foi possível abrir o seletor de arquivos.',
                    ),
            });
    }

    removeProgram(program: ProgramConfig): void {
        this.programs.update((prev) => prev.filter((p) => p !== program));
        if (this.selectedProgram() === program) {
            const remaining = this.programs();
            this.selectedProgram.set(remaining.length > 0 ? remaining[0] : null);
        }
    }

    selectProgram(program: ProgramConfig): void {
        this.selectedProgram.set(program);
    }

    updateSelectedProgram(updated: ProgramConfig): void {
        const current = this.selectedProgram();
        if (!current)
            return;

        this.programs.update((prev) => prev.map((item) => (item === current ? updated : item)));
        this.selectedProgram.set(updated);
    }

    save(): void {
        const sortedPrograms = this.programs()
            .map((program) => {
                const sortedActions = [...(program.actions || [])].sort((a, b) =>
                    (a.route || '').localeCompare(b.route || '', undefined, {
                        numeric: true,
                        sensitivity: 'base',
                    }),
                );

                return {
                    ...program,
                    actions: sortedActions,
                };
            })
            .sort((a, b) => {
                const nameA = this.getFileName(a.path);
                const nameB = this.getFileName(b.path);
                const nameComparison = nameA.localeCompare(nameB, undefined, {
                    numeric: true,
                    sensitivity: 'base',
                });

                if (nameComparison !== 0)
                    return nameComparison;

                return a.id.localeCompare(b.id);
            });

        this.programs.set(sortedPrograms);

        this.loading.set(true);
        this.configService
            .saveProgramConfigs(sortedPrograms)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações dos programas.',
                    ),
            });
    }

    private getFileName(path: string): string {
        if (!path)
            return '';

        const parts = path.split(/[/\\]/);
        return parts[parts.length - 1] || path;
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
