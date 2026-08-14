using AmenoLink.Dtos;
using AmenoLink.Managers.Configuration;
using AmenoLink.Managers.Program;

namespace AmenoLink.Interfaces.Managers.Program;

internal interface IProgramRunner : IDisposable
{
    Task<ActionResponse> Execute(ProgramConfig.Action action, ActionRequest request);
    void RemoveInstance(ProcessInstance instance);
}
