import { Injectable, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, finalize } from 'rxjs';
import { ConfigurationService } from './configuration.service';
import { TopicConfig } from '../models/topic-config.model';
import { SubscribedClient } from '../models/subscribed-client.model';
import { TopicMessage } from '../models/topic-message.model';
import { AlertDialogComponent } from '../components/alert-dialog/alert-dialog.component';

@Injectable({
    providedIn: 'root',
})
export class TopicService {
    private readonly configService = inject(ConfigurationService);
    private readonly dialog = inject(MatDialog);

    readonly topicConfigs = signal<TopicConfig[]>([]);
    readonly selectedTopicConfig = signal<TopicConfig | null>(null);
    private originalTopicJson: string = '[]';
    readonly isModified = signal<boolean>(false);
    readonly loading = signal<boolean>(false);

    load(): void {
        if (this.loading())
            return;

        this.loading.set(true);
        this.configService.topics
            .get()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (data) => {
                    const list = data ?? [];
                    this.originalTopicJson = JSON.stringify(list);
                    this.topicConfigs.set(list);
                    this.isModified.set(false);

                    const currentSelected = this.selectedTopicConfig();
                    if (currentSelected?.name) {
                        const matched = list.find((topic) => topic.name === currentSelected.name);
                        this.selectedTopicConfig.set(matched || (list.length > 0 ? list[0] : null));
                    } else {
                        this.selectedTopicConfig.set(list.length > 0 ? list[0] : null);
                    }
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Carregar',
                        err?.message || 'Não foi possível carregar as configurações de tópicos.',
                    ),
            });
    }

    addTopicConfig(name: string): void {
        const key = name.trim();
        if (!key)
            return;

        const exists = this.topicConfigs().some((topic) => topic.name === key);
        if (exists) {
            this.showErrorDialog('Tópico Existente', `O tópico '${key}' já existe.`);
            return;
        }

        const newConfig: TopicConfig = {
            name: key,
        };

        this.topicConfigs.update((prev) => [...prev, newConfig]);
        this.selectedTopicConfig.set(newConfig);
        this.checkModified();
    }

    removeTopicConfig(config: TopicConfig): void {
        this.topicConfigs.update((prev) => prev.filter((topic) => topic !== config));
        if (this.selectedTopicConfig() === config) {
            const remaining = this.topicConfigs();
            this.selectedTopicConfig.set(remaining.length > 0 ? remaining[0] : null);
        }
        this.checkModified();
    }

    selectTopicConfig(config: TopicConfig): void {
        this.selectedTopicConfig.set(config);
    }

    updateSelectedTopicConfig(updated: TopicConfig): void {
        const current = this.selectedTopicConfig();
        if (!current)
            return;

        this.topicConfigs.update((prev) => prev.map((item) => (item === current ? updated : item)));
        this.selectedTopicConfig.set(updated);
        this.checkModified();
    }

    private checkModified(): void {
        const currentJson = JSON.stringify(this.topicConfigs());
        this.isModified.set(currentJson !== this.originalTopicJson);
    }

    save(): void {
        const sortedConfigs = [...this.topicConfigs()].sort((topicA, topicB) =>
            topicA.name.localeCompare(topicB.name, undefined, {
                numeric: true,
                sensitivity: 'base',
            }),
        );

        this.topicConfigs.set(sortedConfigs);

        this.loading.set(true);
        this.configService.topics
            .save(sortedConfigs)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: () => {
                    this.originalTopicJson = JSON.stringify(sortedConfigs);
                    this.isModified.set(false);
                },
                error: (err) =>
                    this.showErrorDialog(
                        'Erro ao Salvar',
                        err?.message || 'Não foi possível salvar as configurações de tópicos.',
                    ),
            });
    }

    getSubscribers(topicName: string): Observable<SubscribedClient[]> {
        return this.configService.topics.getSubscribers(topicName);
    }

    getRecentMessages(topicName: string): Observable<TopicMessage[]> {
        return this.configService.topics.getRecentMessages(topicName);
    }

    private showErrorDialog(title: string, message: string): void {
        this.dialog.open(AlertDialogComponent, {
            data: { title, message },
        });
    }
}
