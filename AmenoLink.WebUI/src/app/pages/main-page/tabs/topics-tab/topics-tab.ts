import { Component, OnInit, computed, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TopicService } from '../../../../services/topic.service';
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
export class TopicsTab implements OnInit {
    protected readonly topicService = inject(TopicService);
    private readonly dialog = inject(MatDialog);

    readonly topicItems = computed<GroupManagerItem[]>(() =>
        this.topicService.topicConfigs().map((config) => ({
            id: config.name,
            name: config.name,
        })),
    );

    ngOnInit(): void {
        this.topicService.load();
    }

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
