namespace AmenoLink.Configurations;

internal record StoreConfig(
    string GroupName,
    StoreConfig.StorePersistenceMode PersistenceMode
)
{
    internal enum StorePersistenceMode
    {
        Ephemeral = 0,
        Memory = 1,
        Disk = 2
    }
}