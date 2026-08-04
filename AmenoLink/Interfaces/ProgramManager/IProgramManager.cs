using AmenoLink.Dtos;

namespace AmenoLink.Interfaces.ProgramManager;

internal interface IProgramManager : IDisposable
{
    void LoadConfigurations();
    Task<ActionResponse> Execute(ActionRequest request);
}