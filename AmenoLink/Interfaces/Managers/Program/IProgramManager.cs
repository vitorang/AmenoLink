using AmenoLink.Dtos;

namespace AmenoLink.Interfaces.Managers.Program;

internal interface IProgramManager : IDisposable
{
    void LoadConfigurations();
    Task<ActionResponse> Execute(ActionRequest request);
}