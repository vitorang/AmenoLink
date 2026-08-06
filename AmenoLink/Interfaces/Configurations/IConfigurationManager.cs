using AmenoLink.Configurations;

namespace AmenoLink.Interfaces.Configurations;

internal interface IConfigurationManager
{
    GeneralConfig General { get; }
    void LoadConfigurations();
    void SaveGeneralConfig(GeneralConfig config);
}
