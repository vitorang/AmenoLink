import { Component, OnInit, computed, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CacheService } from '../../../../services/cache.service';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { CacheDetails } from './components/cache-details/cache-details';
import { TextPromptModal, TextPromptModalData } from '../../../../components/text-prompt-modal/text-prompt-modal';
import { EmptyState } from '../../../../components/empty-state/empty-state';

@Component({
    selector: 'app-caches-tab',
    imports: [GroupManager, CacheDetails, MatDialogModule, EmptyState],
    templateUrl: './caches-tab.html',
    styleUrl: './caches-tab.scss',
})
export class CachesTab implements OnInit {
    protected readonly cacheService = inject(CacheService);
    private readonly dialog = inject(MatDialog);

    readonly cacheItems = computed<GroupManagerItem[]>(() =>
        this.cacheService.cacheConfigs().map((config) => ({
            id: config.groupName,
            name: config.groupName,
        })),
    );

    ngOnInit(): void {
        this.cacheService.load();
    }

    onAdd(): void {
        const dialogRef = this.dialog.open<TextPromptModal, TextPromptModalData, string>(
            TextPromptModal,
            {
                data: {
                    title: 'Novo Grupo de Cache',
                    label: 'Grupo',
                    icon: 'hourglass_empty',
                    confirmButtonText: 'Criar Grupo',
                },
            },
        );

        dialogRef.afterClosed().subscribe((groupName) => {
            if (!groupName)
                return;

            this.cacheService.addCacheConfig(groupName);
        });
    }

    onRemove(groupName: string): void {
        const config = this.cacheService.cacheConfigs().find((c) => c.groupName === groupName);
        if (config)
            this.cacheService.removeCacheConfig(config);
    }

    onSelect(item: GroupManagerItem): void {
        const config = this.cacheService.cacheConfigs().find((c) => c.groupName === item.id);
        if (config)
            this.cacheService.selectCacheConfig(config);
    }
}
