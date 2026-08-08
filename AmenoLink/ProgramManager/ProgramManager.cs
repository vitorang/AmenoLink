using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.Interfaces.TopicManager;

namespace AmenoLink.ProgramManager;

internal class ProgramManager(ITopicManager topicManager) : IProgramManager
{
    private readonly Dictionary<ProgramConfig, IProgramRunner> runners = [];
    private readonly Dictionary<string, (ProgramConfig Program, ProgramConfig.Action Action)> routeMap = [];

    public void LoadConfigurations()
    {
        var configs = ConfigPathProvider.Program.LoadConfigs();

        lock (runners)
        {
            foreach (var runner in runners.Values)
                runner.Dispose();

            runners.Clear();
        }

        lock (routeMap)
        {
            routeMap.Clear();
            foreach (var program in configs)
            {
                foreach (var action in program.Actions)
                {
                    routeMap[action.Route] = (program, action);
                }
            }
        }
    }

    public async Task<ActionResponse> Execute(ActionRequest request)
    {
        (ProgramConfig Program, ProgramConfig.Action Action) routeData;

        lock (routeMap)
        {
            if (!routeMap.TryGetValue(request.Route, out routeData))
            {
                return new ActionResponse(
                    Previous: request,
                    Success: false,
                    Error: new ActionError(Constants.ActionNotFound, $"Nenhuma ação encontrada para a rota '{request.Route}'.")
                );
            }
        }

        IProgramRunner runner;
        lock (runners)
        {
            if (!runners.TryGetValue(routeData.Program, out runner!))
            {
                runner = new ProgramRunner(routeData.Program);
                runners[routeData.Program] = runner;
            }
        }

        var response = await runner.Execute(routeData.Action, request);
        var topicMessage = new TopicMessage(request.Route, response, Previous: request, AppName: response.AppName);
        if (topicManager.Exists(request.Route))
            _ = topicManager.Publish(request.Route, topicMessage);

        return response;
    }

    public void Dispose()
    {
        lock (runners)
        {
            foreach (var runner in runners.Values)
                runner.Dispose();

            runners.Clear();
        }
    }
}
