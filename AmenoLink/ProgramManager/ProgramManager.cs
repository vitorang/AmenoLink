using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;

namespace AmenoLink.ProgramManager;

internal class ProgramManager : IProgramManager
{
    private readonly Dictionary<ProgramConfig, IProgramRunner> runners = [];
    private readonly Dictionary<string, (ProgramConfig Program, ProgramConfig.Handler Handler)> routeMap = [];

    public void LoadConfigurations()
    {
        var configs = ConfigPathProvider.LoadProgramConfigs();
        lock (routeMap)
        {
            routeMap.Clear();
            foreach (var program in configs)
            {
                foreach (var handler in program.Handlers)
                {
                    routeMap[handler.Route] = (program, handler);
                }
            }
        }
    }

    public async Task<ActionResponse> Execute(ActionRequest request)
    {
        (ProgramConfig Program, ProgramConfig.Handler Handler) routeData;

        lock (routeMap)
        {
            if (!routeMap.TryGetValue(request.Route, out routeData))
            {
                return new ActionResponse(
                    ActionRequest: request,
                    Success: false,
                    ErrorType: Constants.ActionNotFound,
                    ErrorMessage: $"Nenhum handler encontrado para a rota '{request.Route}'."
                );
            }
        }

        IProgramRunner runner;
        lock (runners)
        {
            if (!runners.TryGetValue(routeData.Program, out runner!))
            {
                runner = new ProgramRunner(this, routeData.Program);
                runners[routeData.Program] = runner;
            }
        }

        return await runner.Execute(routeData.Handler, request);
    }
}
