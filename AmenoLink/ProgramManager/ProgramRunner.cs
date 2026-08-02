using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;

namespace AmenoLink.ProgramManager;

internal class ProgramRunner(ProgramConfig config) : IProgramRunner
{
    private readonly SemaphoreSlim semaphore = new(config.MaxInstances, config.MaxInstances);
    private readonly List<ProcessInstance> instances = [];

    public async Task<ActionResponse> Execute(ProgramConfig.Handler handler, ActionRequest request)
    {
        await semaphore.WaitAsync();

        try
        {
            ProcessInstance instance = GetOrCreateInstance();
            return await Task.Run(() => instance.Execute(handler, request));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private ProcessInstance GetOrCreateInstance()
    {
        lock (instances)
        {
            var freeInstance = instances.FirstOrDefault(i => !i.InUse);
            if (freeInstance != null)
                return freeInstance;

            var newInstance = new ProcessInstance(this, config);
            instances.Add(newInstance);
            return newInstance;
        }
    }

    public void RemoveInstance(ProcessInstance instance)
    {
        lock (instances)
        {
            instances.Remove(instance);
        }
    }
}
