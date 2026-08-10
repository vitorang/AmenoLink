import { Component, OnInit, inject } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { GeneralTab } from './tabs/general-tab/general-tab';
import { ProgramsTab } from './tabs/programs-tab/programs-tab';
import { CachesTab } from './tabs/caches-tab/caches-tab';
import { TopicsTab } from './tabs/topics-tab/topics-tab';
import { StoresTab } from './tabs/stores-tab/stores-tab';
import { GeneralService } from '../../services/general.service';
import { ProgramsService } from '../../services/programs.service';
import { CacheService } from '../../services/cache.service';
import { TopicService } from '../../services/topic.service';

export type TabAlias = 'general' | 'programs' | 'caches' | 'topics' | 'stores';

@Component({
    selector: 'app-main-page',
    imports: [MatTabsModule, MatButtonModule, MatIconModule, GeneralTab, ProgramsTab, CachesTab, TopicsTab, StoresTab],
    templateUrl: './main-page.html',
    styleUrl: './main-page.scss',
})
export class MainPage implements OnInit {
    protected readonly generalService = inject(GeneralService);
    protected readonly programsService = inject(ProgramsService);
    protected readonly cacheService = inject(CacheService);
    protected readonly topicService = inject(TopicService);

    activeTab: TabAlias = 'general';

    readonly tabIndexMap: Record<TabAlias, number> = {
        general: 0,
        programs: 1,
        caches: 2,
        topics: 3,
        stores: 4,
    };

    readonly tabAliases: TabAlias[] = ['general', 'programs', 'caches', 'topics', 'stores'];

    get isCurrentTabModified(): boolean {
        if (this.activeTab === 'general')
            return this.generalService.isModified();
        if (this.activeTab === 'programs')
            return this.programsService.isModified();
        if (this.activeTab === 'caches')
            return this.cacheService.isModified();
        if (this.activeTab === 'topics')
            return this.topicService.isModified();
        return false;
    }

    get activeTabIndex(): number {
        return this.tabIndexMap[this.activeTab];
    }

    set activeTabIndex(index: number) {
        this.activeTab = this.tabAliases[index] ?? 'general';
    }

    ngOnInit(): void {
        this.generalService.load();
        this.programsService.load();
    }

    onUndo(): void {
        if (this.activeTab === 'general')
            this.generalService.load();
        else if (this.activeTab === 'programs')
            this.programsService.load();
        else if (this.activeTab === 'caches')
            this.cacheService.load();
        else if (this.activeTab === 'topics')
            this.topicService.load();
    }

    onSave(): void {
        if (this.activeTab === 'general')
            this.generalService.save();
        else if (this.activeTab === 'programs')
            this.programsService.save();
        else if (this.activeTab === 'caches')
            this.cacheService.save();
        else if (this.activeTab === 'topics')
            this.topicService.save();
    }
}
