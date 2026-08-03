import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CacheEntryItem {
    key: string;
    value: unknown;
}

@Injectable({
    providedIn: 'root',
})
export class CacheDataService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'http://localhost:13545/api/cache';

    getAllEntries(groupKey: string): Observable<Record<string, unknown>> {
        return this.http.get<Record<string, unknown>>(`${this.baseUrl}/all`, {
            params: { groupKey },
        });
    }

    getValue(groupKey: string, key: string): Observable<unknown> {
        return this.http.get<unknown>(`${this.baseUrl}`, {
            params: { groupKey, key },
        });
    }

    deleteEntry(groupKey: string, key: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}`, {
            params: { groupKey, key },
        });
    }
}
