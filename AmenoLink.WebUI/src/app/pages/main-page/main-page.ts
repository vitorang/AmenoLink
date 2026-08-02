import { Component, OnInit, inject } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HubsTab } from './tabs/hubs-tab/hubs-tab';
import { ProgramsTab } from './tabs/programs-tab/programs-tab';
import { ProgramsService } from '../../services/programs.service';

export type TabAlias = 'programs' | 'hubs';

@Component({
  selector: 'app-main-page',
  imports: [MatTabsModule, MatButtonModule, MatIconModule, ProgramsTab, HubsTab],
  templateUrl: './main-page.html',
  styleUrl: './main-page.scss',
})
export class MainPage implements OnInit {
  protected readonly programsService = inject(ProgramsService);
  activeTab: TabAlias = 'programs';

  readonly tabIndexMap: Record<TabAlias, number> = {
    programs: 0,
    hubs: 1,
  };

  readonly tabAliases: TabAlias[] = ['programs', 'hubs'];

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
    if (this.activeTab === 'programs') {
      this.programsService.load();
    }
  }

  onSave(): void {
    if (this.activeTab === 'programs') {
      this.programsService.save();
    }
  }
}
