export enum StorePersistenceMode {
    Ephemeral = 0,
    Memory = 1,
    Disk = 2,
}

export interface StoreConfig {
    groupName: string;
    persistenceMode: StorePersistenceMode;
}
