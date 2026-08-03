import { Component, OnInit, inject } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProgramsTab } from './tabs/programs-tab/programs-tab';
import { CacheTab } from './tabs/cache-tab/cache-tab';
import { StoreTab } from './tabs/store-tab/store-tab';
import { ProgramsService } from '../../services/programs.service';
import { CacheService } from '../../services/cache.service';

export type TabAlias = 'programs' | 'cache' | 'store';

@Component({
    selector: 'app-main-page',
    imports: [MatTabsModule, MatButtonModule, MatIconModule, ProgramsTab, CacheTab, StoreTab],
    templateUrl: './main-page.html',
    styleUrl: './main-page.scss',
})
export class MainPage implements OnInit {
    protected readonly programsService = inject(ProgramsService);
    protected readonly cacheService = inject(CacheService);

    activeTab: TabAlias = 'programs';

    readonly tabIndexMap: Record<TabAlias, number> = {
        programs: 0,
        cache: 1,
        store: 2,
    };

    readonly tabAliases: TabAlias[] = ['programs', 'cache', 'store'];

    get activeTabIndex(): number {
        return this.tabIndexMap[this.activeTab];
    }

    set activeTabIndex(index: number) {
        this.activeTab = this.tabAliases[index] ?? 'programs';
    }

    ngOnInit(): void {
        this.programsService.load();
    }

    onUndo(): void {
        if (this.activeTab === 'programs')
            this.programsService.load();
        else if (this.activeTab === 'cache')
            this.cacheService.load();
    }

    onSave(): void {
        if (this.activeTab === 'programs')
            this.programsService.save();
        else if (this.activeTab === 'cache')
            this.cacheService.save();
    }
}
