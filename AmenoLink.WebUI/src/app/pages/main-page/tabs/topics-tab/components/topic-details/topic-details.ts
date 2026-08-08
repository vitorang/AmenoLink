import { Component, input, output, inject, signal, effect } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { TopicConfig } from '../../../../../../models/topic-config.model';
import { SubscribedClient } from '../../../../../../models/subscribed-client.model';
import { TopicService } from '../../../../../../services/topic.service';

@Component({
    selector: 'app-topic-details',
    imports: [MatIconModule, MatButtonModule, MatTableModule],
    templateUrl: './topic-details.html',
    styleUrl: './topic-details.scss',
})
export class TopicDetails {
    private readonly topicService = inject(TopicService);

    readonly config = input.required<TopicConfig>();
    readonly configChange = output<TopicConfig>();

    readonly subscribers = signal<SubscribedClient[] | null>(null);
    readonly loadingSubscribers = signal<boolean>(false);
    readonly displayedColumns: string[] = ['appName', 'connectionId'];

    private currentTopicName: string | null = null;

    constructor() {
        effect(() => {
            const name = this.config().name;
            if (this.currentTopicName !== name) {
                this.currentTopicName = name;
                this.subscribers.set(null);
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
}
