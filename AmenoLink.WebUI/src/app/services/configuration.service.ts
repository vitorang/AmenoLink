import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProgramConfig } from '../models/program-config.model';
import { CacheConfig } from '../models/cache-config.model';
import { TopicConfig } from '../models/topic-config.model';
import { GeneralConfig } from '../models/general-config.model';
import { SubscribedClient } from '../models/subscribed-client.model';

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

    getCacheConfigs(): Observable<CacheConfig[]> {
        return this.http.get<CacheConfig[]>(`${this.baseUrl}/cache`);
    }

    saveCacheConfigs(configs: CacheConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/cache`, configs);
    }

    getTopicConfigs(): Observable<TopicConfig[]> {
        return this.http.get<TopicConfig[]>(`${this.baseUrl}/topics`);
    }

    saveTopicConfigs(configs: TopicConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/topics`, configs);
    }

    getGeneralConfig(): Observable<GeneralConfig> {
        return this.http.get<GeneralConfig>(`${this.baseUrl}/general`);
    }

    saveGeneralConfig(config: GeneralConfig): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/general`, config);
    }

    getTopicSubscribers(topicName: string): Observable<SubscribedClient[]> {
        return this.http.get<SubscribedClient[]>(`${this.baseUrl}/topic/subscribers`, {
            params: { topicName },
        });
    }

    selectExecutable(currentPath?: string): Observable<string | null> {
        let url = `${this.baseUrl}/select-executable`;
        if (currentPath)
            url += `?currentPath=${encodeURIComponent(currentPath)}`;

        return this.http.get<string | null>(url);
    }
}
