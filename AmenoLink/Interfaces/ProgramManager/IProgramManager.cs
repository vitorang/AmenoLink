using AmenoLink.Dtos;

namespace AmenoLink.Interfaces.ProgramManager;

internal interface IProgramManager
{
    void LoadConfigurations();
    Task<ActionResponse> Execute(ActionRequest request);
}