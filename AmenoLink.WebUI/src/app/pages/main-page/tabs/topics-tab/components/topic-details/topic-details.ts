import { Component, input, output, inject, signal, effect, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { TopicConfig } from '../../../../../../models/topic-config.model';
import { SubscribedClient } from '../../../../../../models/subscribed-client.model';
import { TopicMessage } from '../../../../../../models/topic-message.model';
import { TopicService } from '../../../../../../services/topic.service';
import { ProgramsService } from '../../../../../../services/programs.service';

@Component({
    selector: 'app-topic-details',
    imports: [MatIconModule, MatButtonModule, MatTableModule, MatExpansionModule],
    templateUrl: './topic-details.html',
    styleUrl: './topic-details.scss',
})
export class TopicDetails {
    private readonly topicService = inject(TopicService);
    private readonly programsService = inject(ProgramsService);

    readonly config = input.required<TopicConfig>();
    readonly configChange = output<TopicConfig>();

    readonly isActionMatch = computed<boolean>(() => {
        const topicName = this.config().name;
        if (!topicName)
            return false;

        return this.programsService
            .programs()
            .some((program) => (program.actions || []).some((action) => action.route === topicName));
    });

    readonly subscribers = signal<SubscribedClient[] | null>(null);
    readonly loadingSubscribers = signal<boolean>(false);
    readonly displayedColumns: string[] = ['appName', 'connectionId'];

    readonly recentMessages = signal<TopicMessage[] | null>(null);
    readonly loadingRecentMessages = signal<boolean>(false);

    private currentTopicName: string | null = null;

    constructor() {
        effect(() => {
            const name = this.config().name;
            if (this.currentTopicName !== name) {
                this.currentTopicName = name;
                this.subscribers.set(null);
                this.recentMessages.set(null);
            }
        });
    }

    onRefreshSubscribers(): void {
        const topicName = this.config().name;
        this.loadingSubscribers.set(true);

        this.topicService
            .getSubscribers(topicName)
            .pipe(
                catchError(() => of([])),
                finalize(() => this.loadingSubscribers.set(false)),
            )
            .subscribe({
                next: (data) => {
                    this.subscribers.set(data || []);
                },
            });
    }

    onRefreshRecentMessages(): void {
        const topicName = this.config().name;
        this.loadingRecentMessages.set(true);

        this.topicService
            .getRecentMessages(topicName)
            .pipe(
                catchError(() => of([])),
                finalize(() => this.loadingRecentMessages.set(false)),
            )
            .subscribe({
                next: (data) => {
                    this.recentMessages.set(data || []);
                },
            });
    }

    formatTime(createdAt?: string): string {
        if (!createdAt)
            return '--:--:--';

        const date = new Date(createdAt);
        if (isNaN(date.getTime()))
            return '--:--:--';

        return date.toLocaleTimeString('pt-BR', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false,
        });
    }

    formatFormattedJson(message: TopicMessage): string {
        try {
            return JSON.stringify(message, null, 2);
        } catch {
            return String(message);
        }
    }
}
