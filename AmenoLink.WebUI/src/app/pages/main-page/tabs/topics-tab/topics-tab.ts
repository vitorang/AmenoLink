import { Component, computed, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TopicService } from '../../../../services/topic.service';
import { ProgramsService } from '../../../../services/programs.service';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { TopicDetails } from './components/topic-details/topic-details';
import { TextPromptModal, TextPromptModalData } from '../../../../components/text-prompt-modal/text-prompt-modal';
import { EmptyState } from '../../../../components/empty-state/empty-state';

@Component({
    selector: 'app-topics-tab',
    imports: [GroupManager, TopicDetails, MatDialogModule, EmptyState],
    templateUrl: './topics-tab.html',
    styleUrl: './topics-tab.scss',
})
export class TopicsTab {
    protected readonly topicService = inject(TopicService);
    protected readonly programsService = inject(ProgramsService);
    private readonly dialog = inject(MatDialog);

    private readonly actionRoutes = computed<Set<string>>(() => {
        const routes = new Set<string>();
        for (const program of this.programsService.programs()) {
            for (const action of program.actions || []) {
                if (action.route)
                    routes.add(action.route);
            }
        }
        return routes;
    });

    readonly topicItems = computed<GroupManagerItem[]>(() => {
        const routes = this.actionRoutes();
        return this.topicService.topicConfigs().map((config) => ({
            id: config.name,
            name: config.name,
            hasAction: routes.has(config.name),
        }));
    });

    onAdd(): void {
        const dialogRef = this.dialog.open<TextPromptModal, TextPromptModalData, string>(
            TextPromptModal,
            {
                data: {
                    title: 'Novo Tópico',
                    label: 'Tópico',
                    icon: 'campaign',
                    confirmButtonText: 'Criar Tópico',
                },
            },
        );

        dialogRef.afterClosed().subscribe((name) => {
            if (!name)
                return;

            this.topicService.addTopicConfig(name);
        });
    }

    onRemove(name: string): void {
        const config = this.topicService.topicConfigs().find((topic) => topic.name === name);
        if (config)
            this.topicService.removeTopicConfig(config);
    }

    onSelect(item: GroupManagerItem): void {
        const config = this.topicService.topicConfigs().find((topic) => topic.name === item.id);
        if (config)
            this.topicService.selectTopicConfig(config);
    }
}
