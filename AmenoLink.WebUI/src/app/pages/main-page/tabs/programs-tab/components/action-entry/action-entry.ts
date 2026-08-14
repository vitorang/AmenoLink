import { Component, computed, inject, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ProgramConfigAction } from '../../../../../../models/program-config.model';
import { TopicService } from '../../../../../../services/topic.service';

@Component({
    selector: 'app-action-entry',
    imports: [MatIconModule, MatButtonModule],
    templateUrl: './action-entry.html',
    styleUrl: './action-entry.scss',
})
export class ActionEntry {
    private readonly topicService = inject(TopicService);

    readonly action = input.required<ProgramConfigAction>();
    readonly remove = output<void>();

    readonly hasTopicMatch = computed<boolean>(() => {
        const route = this.action().route;
        if (!route)
            return false;

        return this.topicService
            .topicConfigs()
            .some((topic) => topic.name === route);
    });

    onRemoveClick(event: MouseEvent): void {
        event.stopPropagation();
        this.remove.emit();
    }
}
