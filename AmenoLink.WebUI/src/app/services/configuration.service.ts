import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProgramConfig } from '../models/program-config.model';
import { PackageInstallInstructions, PackageVersion } from '../models/project-config.model';
import { CacheConfig } from '../models/cache-config.model';
import { TopicConfig } from '../models/topic-config.model';
import { SpaConfig } from '../models/spa-config.model';
import { GeneralConfig } from '../models/general-config.model';
import { SubscribedClient } from '../models/subscribed-client.model';
import { TopicMessage } from '../models/topic-message.model';

export class GeneralConfigEndpoint {
    constructor(private readonly http: HttpClient, private readonly baseUrl: string) {}

    get(): Observable<GeneralConfig> {
        return this.http.get<GeneralConfig>(`${this.baseUrl}/general`);
    }

    save(config: GeneralConfig): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/general`, config);
    }

    selectPackageManifest(type: string, currentPath?: string): Observable<string | null> {
        let url = `${this.baseUrl}/general/packages/manifest?type=${encodeURIComponent(type)}`;
        if (currentPath)
            url += `&currentPath=${encodeURIComponent(currentPath)}`;

        return this.http.get<string | null>(url);
    }

    getPackageVersion(manifestPath: string, type: string): Observable<PackageVersion> {
        const url = `${this.baseUrl}/general/packages/version?manifestPath=${encodeURIComponent(manifestPath)}&type=${encodeURIComponent(type)}`;
        return this.http.get<PackageVersion>(url);
    }

    getPackageInstallInstructions(): Observable<PackageInstallInstructions> {
        return this.http.get<PackageInstallInstructions>(`${this.baseUrl}/general/packages/instructions`);
    }
}

export class ProgramConfigEndpoint {
    constructor(private readonly http: HttpClient, private readonly baseUrl: string) {}

    get(): Observable<ProgramConfig[]> {
        return this.http.get<ProgramConfig[]>(`${this.baseUrl}/programs`);
    }

    save(configs: ProgramConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/programs`, configs);
    }

    selectExecutable(currentPath?: string): Observable<string | null> {
        let url = `${this.baseUrl}/programs/select-executable`;
        if (currentPath)
            url += `?currentPath=${encodeURIComponent(currentPath)}`;

        return this.http.get<string | null>(url);
    }
}

export class CacheConfigEndpoint {
    constructor(private readonly http: HttpClient, private readonly baseUrl: string) {}

    get(): Observable<CacheConfig[]> {
        return this.http.get<CacheConfig[]>(`${this.baseUrl}/cache`);
    }

    save(configs: CacheConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/cache`, configs);
    }

    getSubscribers(groupName: string): Observable<SubscribedClient[]> {
        return this.http.get<SubscribedClient[]>(`${this.baseUrl}/cache/subscribers`, {
            params: { groupName },
        });
    }
}

export class TopicConfigEndpoint {
    constructor(private readonly http: HttpClient, private readonly baseUrl: string) {}

    get(): Observable<TopicConfig[]> {
        return this.http.get<TopicConfig[]>(`${this.baseUrl}/topics`);
    }

    save(configs: TopicConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/topics`, configs);
    }

    getSubscribers(topicName: string): Observable<SubscribedClient[]> {
        return this.http.get<SubscribedClient[]>(`${this.baseUrl}/topic/subscribers`, {
            params: { topicName },
        });
    }

    getRecentMessages(topicName: string): Observable<TopicMessage[]> {
        return this.http.get<TopicMessage[]>(`${this.baseUrl}/topic/recent`, {
            params: { topicName },
        });
    }
}

export class SpaConfigEndpoint {
    constructor(private readonly http: HttpClient, private readonly baseUrl: string) {}

    get(): Observable<SpaConfig[]> {
        return this.http.get<SpaConfig[]>(`${this.baseUrl}/spa`);
    }

    save(configs: SpaConfig[]): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/spa`, configs);
    }

    selectFolder(currentPath?: string): Observable<string | null> {
        let url = `${this.baseUrl}/spa/select-folder`;
        if (currentPath)
            url += `?currentPath=${encodeURIComponent(currentPath)}`;

        return this.http.get<string | null>(url);
    }
}

@Injectable({
    providedIn: 'root',
})
export class ConfigurationService {
    private readonly http = inject(HttpClient);
    readonly hostUrl = 'http://localhost:13545';
    private readonly baseUrl = `${this.hostUrl}/api/config`;

    readonly general = new GeneralConfigEndpoint(this.http, this.baseUrl);
    readonly programs = new ProgramConfigEndpoint(this.http, this.baseUrl);
    readonly cache = new CacheConfigEndpoint(this.http, this.baseUrl);
    readonly topics = new TopicConfigEndpoint(this.http, this.baseUrl);
    readonly spa = new SpaConfigEndpoint(this.http, this.baseUrl);

    openUrl(url: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/open-url`, { url });
    }
}
