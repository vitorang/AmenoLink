import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProjectConfig, PROJECT_TYPE_METADATA, ProjectTypeMetadata, PackageVersion } from '../../../../../../models/project-config.model';

@Component({
    selector: 'app-project-entry',
    imports: [MatIconModule, MatButtonModule, MatTooltipModule],
    templateUrl: './project-entry.html',
    styleUrl: './project-entry.scss',
})
export class ProjectEntry {
    readonly project = input.required<ProjectConfig>();
    readonly packageVersion = input<PackageVersion | undefined>();
    readonly loadingVersion = input<boolean>(false);
    readonly remove = output<void>();
    readonly installOrUpdate = output<void>();

    readonly typeMetadata = computed<ProjectTypeMetadata>(() => {
        const type = this.project().type;
        const metadata = PROJECT_TYPE_METADATA[type];
        if (!metadata)
            throw new Error(`Tipo de projeto desconhecido: ${type}`);

        return metadata;
    });

    onInstallOrUpdateClick(event: MouseEvent): void {
        event.stopPropagation();
        this.installOrUpdate.emit();
    }

    onRemoveClick(event: MouseEvent): void {
        event.stopPropagation();
        this.remove.emit();
    }
}
