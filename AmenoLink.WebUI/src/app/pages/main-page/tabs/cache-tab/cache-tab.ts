import { Component, OnInit, computed, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CacheService } from '../../../../services/cache.service';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { CacheDetails } from './components/cache-details/cache-details';
import { CacheGroupRegisterModal } from './components/cache-group-register-modal/cache-group-register-modal';

@Component({
    selector: 'app-cache-tab',
    imports: [GroupManager, CacheDetails, MatIconModule, MatDialogModule],
    templateUrl: './cache-tab.html',
    styleUrl: './cache-tab.scss',
})
export class CacheTab implements OnInit {
    protected readonly cacheService = inject(CacheService);
    private readonly dialog = inject(MatDialog);

    readonly cacheItems = computed<GroupManagerItem[]>(() =>
        this.cacheService.cacheConfigs().map((config) => ({
            id: config.groupKey,
            name: config.groupKey,
        })),
    );

    ngOnInit(): void {
        this.cacheService.load();
    }

    onAdd(): void {
        const dialogRef = this.dialog.open<CacheGroupRegisterModal, void, string>(
            CacheGroupRegisterModal,
        );

        dialogRef.afterClosed().subscribe((groupKey) => {
            if (!groupKey)
                return;

            this.cacheService.addCacheConfig(groupKey);
        });
    }

    onRemove(groupKey: string): void {
        const config = this.cacheService.cacheConfigs().find((c) => c.groupKey === groupKey);
        if (config)
            this.cacheService.removeCacheConfig(config);
    }

    onSelect(item: GroupManagerItem): void {
        const config = this.cacheService.cacheConfigs().find((c) => c.groupKey === item.id);
        if (config)
            this.cacheService.selectCacheConfig(config);
    }

    onRename(): void {
        const current = this.cacheService.selectedCacheConfig();
        if (!current)
            return;

        const dialogRef = this.dialog.open<CacheGroupRegisterModal, { groupKey?: string }, string>(
            CacheGroupRegisterModal,
            { data: { groupKey: current.groupKey } },
        );

        dialogRef.afterClosed().subscribe((newGroupKey) => {
            if (!newGroupKey)
                return;

            this.cacheService.renameCacheConfig(current.groupKey, newGroupKey);
        });
    }
}
