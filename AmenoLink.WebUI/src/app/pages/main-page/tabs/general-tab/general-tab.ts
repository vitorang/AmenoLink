import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { GeneralService } from '../../../../services/general.service';
import { handleInputBlur, sanitizeInteger } from '../../../../utils/number.utils';

@Component({
    selector: 'app-general-tab',
    imports: [
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSlideToggleModule,
        MatIconModule,
    ],
    templateUrl: './general-tab.html',
    styleUrl: './general-tab.scss',
})
export class GeneralTab {
    protected readonly generalService = inject(GeneralService);

    onToggleStartMinimizedToTray(): void {
        const currentValue = this.generalService.generalConfig().startMinimizedToTray;
        this.generalService.updateGeneralConfig({ startMinimizedToTray: !currentValue });
    }

    onStartMinimizedToTrayChange(startMinimizedToTray: boolean): void {
        this.generalService.updateGeneralConfig({ startMinimizedToTray });
    }

    onMaxMessageDepthChange(value: number | null): void {
        const sanitizedValue = sanitizeInteger(value, 1);
        this.generalService.updateGeneralConfig({ maxMessageDepth: sanitizedValue });
    }

    onMaxTopicHistorySizeChange(value: number | null): void {
        const sanitizedValue = sanitizeInteger(value, 1);
        this.generalService.updateGeneralConfig({ maxTopicHistorySize: sanitizedValue });
    }

    onMaxMessageDepthBlur(event: FocusEvent): void {
        const sanitizedValue = handleInputBlur(event, 1);
        this.generalService.updateGeneralConfig({ maxMessageDepth: sanitizedValue });
    }

    onMaxTopicHistorySizeBlur(event: FocusEvent): void {
        const sanitizedValue = handleInputBlur(event, 1);
        this.generalService.updateGeneralConfig({ maxTopicHistorySize: sanitizedValue });
    }
}
