import { Component, OnInit, computed, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CacheService } from '../../../../services/cache.service';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { CacheDetails } from './components/cache-details/cache-details';
import { CacheGroupRegisterModal } from './components/cache-group-register-modal/cache-group-register-modal';
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
        const dialogRef = this.dialog.open<CacheGroupRegisterModal, void, string>(
            CacheGroupRegisterModal,
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

    onRename(): void {
        const current = this.cacheService.selectedCacheConfig();
        if (!current)
            return;

        const dialogRef = this.dialog.open<CacheGroupRegisterModal, { groupName?: string }, string>(
            CacheGroupRegisterModal,
            { data: { groupName: current.groupName } },
        );

        dialogRef.afterClosed().subscribe((newGroupName) => {
            if (!newGroupName)
                return;

            this.cacheService.renameCacheConfig(current.groupName, newGroupName);
        });
    }
}
