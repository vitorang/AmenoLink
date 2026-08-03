import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface GroupManagerItem {
    id: string;
    name: string;
    count?: number;
}

@Component({
    selector: 'app-group-manager',
    imports: [MatButtonModule, MatIconModule],
    templateUrl: './group-manager.html',
    styleUrl: './group-manager.scss',
})
export class GroupManager {
    readonly items = input.required<GroupManagerItem[]>();
    readonly selectedId = input<string | null>(null);

    readonly itemIcon = input.required<string>();
    readonly badgeIcon = input.required<string>();
    readonly emptyText = input<string>('Nenhum item cadastrado.');

    readonly add = output<void>();
    readonly remove = output<string>();
    readonly select = output<GroupManagerItem>();

    onAdd(): void {
        this.add.emit();
    }

    onRemove(): void {
        const currentId = this.selectedId();
        if (currentId)
            this.remove.emit(currentId);
    }

    onSelect(item: GroupManagerItem): void {
        this.select.emit(item);
    }
}
