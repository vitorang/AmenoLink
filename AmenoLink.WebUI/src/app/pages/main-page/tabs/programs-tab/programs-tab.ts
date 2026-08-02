import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProgramsService } from '../../../../services/programs.service';
import { ProgramEntry } from './components/program-entry/program-entry';
import { ProgramDetails } from './components/program-details/program-details';

@Component({
  selector: 'app-programs-tab',
  imports: [MatButtonModule, MatIconModule, ProgramEntry, ProgramDetails],
  templateUrl: './programs-tab.html',
  styleUrl: './programs-tab.scss',
})
export class ProgramsTab {
  protected readonly programsService = inject(ProgramsService);

  onAdd(): void {
    this.programsService.addProgram();
  }

  onRemove(): void {
    const selected = this.programsService.selectedProgram();
    if (selected) {
      this.programsService.removeProgram(selected);
    }
  }
}
