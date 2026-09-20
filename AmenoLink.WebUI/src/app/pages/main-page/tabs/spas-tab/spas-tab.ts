import { Component, computed, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SpaService } from '../../../../services/spa.service';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { SpaDetails } from './components/spa-details/spa-details';
import { TextPromptModal, TextPromptModalData } from '../../../../components/text-prompt-modal/text-prompt-modal';
import { EmptyState } from '../../../../components/empty-state/empty-state';

@Component({
    selector: 'app-spas-tab',
    imports: [GroupManager, SpaDetails, MatDialogModule, EmptyState],
    templateUrl: './spas-tab.html',
    styleUrl: './spas-tab.scss',
})
export class SpasTab {
    protected readonly spaService = inject(SpaService);
    private readonly dialog = inject(MatDialog);

    readonly spaItems = computed<GroupManagerItem[]>(() =>
        this.spaService.spaConfigs().map((config) => ({
            id: config.route,
            name: config.route,
        })),
    );

    onAdd(): void {
        const dialogRef = this.dialog.open<TextPromptModal, TextPromptModalData, string>(
            TextPromptModal,
            {
                data: {
                    title: 'Novo SPA',
                    label: 'Rota',
                    icon: 'web',
                    confirmButtonText: 'Criar SPA',
                    pattern: /^[a-zA-Z0-9_-]+$/,
                },
            },
        );

        dialogRef.afterClosed().subscribe((route) => {
            if (!route)
                return;

            this.spaService.addSpaConfig(route);
        });
    }

    onRemove(route: string): void {
        const config = this.spaService.spaConfigs().find((c) => c.route === route);
        if (config)
            this.spaService.removeSpaConfig(config);
    }

    onSelect(item: GroupManagerItem): void {
        const config = this.spaService.spaConfigs().find((c) => c.route === item.id);
        if (config)
            this.spaService.selectSpaConfig(config);
    }
}
