import { Component, computed, inject } from '@angular/core';
import { ProgramsService } from '../../../../services/programs.service';
import { ProgramDetails } from './components/program-details/program-details';
import { GroupManager, GroupManagerItem } from '../../components/group-manager/group-manager';
import { EmptyState } from '../../../../components/empty-state/empty-state';

@Component({
    selector: 'app-programs-tab',
    imports: [ProgramDetails, GroupManager, EmptyState],
    templateUrl: './programs-tab.html',
    styleUrl: './programs-tab.scss',
})
export class ProgramsTab {
    protected readonly programsService = inject(ProgramsService);

    readonly programItems = computed<GroupManagerItem[]>(() =>
        this.programsService.programs().map((p) => {
            const parts = p.path.split(/[/\\]/);
            const name = parts[parts.length - 1] || p.path;
            return {
                id: p.id,
                name,
                count: p.actions.length,
            };
        }),
    );

    onAdd(): void {
        this.programsService.addProgram();
    }

    onRemove(id: string): void {
        const program = this.programsService.programs().find((p) => p.id === id);
        if (program)
            this.programsService.removeProgram(program);
    }

    onSelect(item: GroupManagerItem): void {
        const program = this.programsService.programs().find((p) => p.id === item.id);
        if (program)
            this.programsService.selectProgram(program);
    }
}
