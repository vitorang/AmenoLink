using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;

namespace AmenoLink.ProgramManager;

internal class ProgramRunner(ProgramConfig config) : IProgramRunner
{
    private readonly SemaphoreSlim semaphore = new(config.MaxInstances, config.MaxInstances);
    private readonly List<ProcessInstance> instances = [];

    public async Task<ActionResponse> Execute(ProgramConfig.Action action, ActionRequest request)
    {
        await semaphore.WaitAsync();

        try
        {
            ProcessInstance instance = GetOrCreateInstance();
            return await Task.Run(() => instance.Execute(action, request));
        }
        finally
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException) { }
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

    public void Dispose()
    {
        lock (instances)
        {
            foreach (var instance in instances.ToList())
                instance.Dispose();

            instances.Clear();
        }

        semaphore.Dispose();
    }
}
