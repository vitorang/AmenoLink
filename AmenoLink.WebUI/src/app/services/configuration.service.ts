import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProgramConfig } from '../models/program-config.model';

@Injectable({
  providedIn: 'root',
})
export class ConfigurationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:13545/api/config';

  getProgramConfigs(): Observable<ProgramConfig[]> {
    return this.http.get<ProgramConfig[]>(`${this.baseUrl}/programs`);
  }

  saveProgramConfigs(configs: ProgramConfig[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/programs`, configs);
  }

  selectExecutable(): Observable<string | null> {
    return this.http.get<string | null>(`${this.baseUrl}/select-executable`);
  }
}
