import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface GroupManagerItem {
    id: string;
    name: string;
    count?: number;
    hasAction?: boolean;
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

    readonly badgeIcon = input.required<string>();
    readonly emptyText = input<string>('Nenhum item cadastrado.');

    readonly add = output<void>();
    readonly remove = output<string>();
    readonly select = output<GroupManagerItem>();

    readonly scrollList = viewChild<ElementRef<HTMLElement>>('scrollList');

    readonly sortedItems = computed<GroupManagerItem[]>(() =>
        [...this.items()].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' }),
        ),
    );

    constructor() {
        effect(() => {
            const id = this.selectedId();
            if (!id)
                return;

            setTimeout(() => {
                const listElement = this.scrollList()?.nativeElement;
                if (!listElement)
                    return;

                const target = listElement.querySelector(`[data-id="${CSS.escape(id)}"]`);
                if (target)
                    target.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            }, 0);
        });
    }

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
