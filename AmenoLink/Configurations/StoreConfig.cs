namespace AmenoLink.Configurations;

internal record StoreConfig(
    string GroupKey,
    StoreConfig.StorePersistenceMode PersistenceMode
)
{
    internal enum StorePersistenceMode
    {
        Ephemeral = 0,
        Memory = 1,
        Disk = 2 // TODO: usar SQLite WAL
    }
}