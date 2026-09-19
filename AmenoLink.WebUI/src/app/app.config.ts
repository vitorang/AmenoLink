import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { MAT_ICON_DEFAULT_OPTIONS } from '@angular/material/icon';
import { MAT_TOOLTIP_DEFAULT_OPTIONS, MatTooltipDefaultOptions } from '@angular/material/tooltip';

import { routes } from './app.routes';

const tooltipDefaults: MatTooltipDefaultOptions = {
    showDelay: 500,
    hideDelay: 0,
    touchendHideDelay: 0,
};

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes),
        provideHttpClient(),
        {
            provide: MAT_ICON_DEFAULT_OPTIONS,
            useValue: { fontSet: 'material-symbols-outlined' },
        },
        {
            provide: MAT_TOOLTIP_DEFAULT_OPTIONS,
            useValue: tooltipDefaults,
        },
    ],
};
