using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.ProgramManager;

namespace AmenoLink.Interfaces.ProgramManager;

internal interface IProgramRunner : IDisposable
{
    Task<ActionResponse> Execute(ProgramConfig.Handler handler, ActionRequest request);
    void RemoveInstance(ProcessInstance instance);
}
