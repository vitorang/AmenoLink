using AmenoLink.Interfaces.Configurations;

namespace AmenoLink.Configurations;

internal class ConfigurationManager : IConfigurationManager
{
    public GeneralConfig General { get; private set; } = new();

    public void LoadConfigurations()
    {
        General = ConfigPathProvider.General.LoadConfig();
    }

    public void SaveGeneralConfig(GeneralConfig config)
    {
        ConfigPathProvider.General.SaveConfig(config);
        General = config;
    }
}
