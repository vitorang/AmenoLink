import { Resources } from './dtos';
import { clientSetup, postJson, AmenoException } from './shared';
import packageJson from '../package.json' with { type: 'json' };

export const PACKAGE_VERSION = packageJson.version;

export class ResourceManager {
    public readonly actions: Set<string> = new Set();
    public readonly caches: Set<string> = new Set();
    public readonly topics: Set<string> = new Set();

    public async ensureReady(): Promise<void> {
        const resources: Resources = {
            actions: Array.from(this.actions),
            caches: Array.from(this.caches),
            topics: Array.from(this.topics),
            version: PACKAGE_VERSION
        };

        const url = `${clientSetup.originUrl}/api/resources/missing`;
        const missingResources = await postJson<Resources>(url, resources);

        if (missingResources.version !== PACKAGE_VERSION)
            throw new AmenoException(
                `Versão incompatível do AmenoLink. Host: ${missingResources.version}, Cliente: ${PACKAGE_VERSION}.`
            );

        const missingItems: string[] = [];

        if (missingResources.actions && missingResources.actions.length > 0)
            missingItems.push(`Actions: ${missingResources.actions.join(', ')}`);

        if (missingResources.caches && missingResources.caches.length > 0)
            missingItems.push(`Caches: ${missingResources.caches.join(', ')}`);

        if (missingResources.topics && missingResources.topics.length > 0)
            missingItems.push(`Topics: ${missingResources.topics.join(', ')}`);

        if (missingItems.length > 0) {
            const details = missingItems.join('\n');
            throw new AmenoException(`Recursos ausentes no AmenoLink:\n${details}`);
        }
    }
}

export const resourceManager = new ResourceManager();

export function ensureReady(): Promise<void> {
    return resourceManager.ensureReady();
}
