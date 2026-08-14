using AmenoLink.Managers.Configuration;

namespace AmenoLink.Interfaces.Managers.Configuration;

internal interface IConfigurationManager
{
    GeneralConfig General { get; }
    void LoadConfigurations();
    void SaveGeneralConfig(GeneralConfig config);
}
