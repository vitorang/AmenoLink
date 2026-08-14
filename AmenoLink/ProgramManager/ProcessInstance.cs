using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.Shared;
using System.Collections.Concurrent;
using System.Diagnostics;
using Timer = System.Threading.Timer;

namespace AmenoLink.ProgramManager;

internal sealed class ProcessInstance(IProgramRunner runner, ProgramConfig config) : IDisposable
{
    public bool InUse => inUse;
    private bool inUse = false;
    private readonly Lock lockObject = new();
    private int isDisposed = 0;
    private Process? process;
    private Timer? idleTimer;
    private Timer? startupTimer;
    private Timer? actionTimer;
    private AutoResetEvent? currentResponseEvent;
    private AutoResetEvent? currentStartupEvent;
    private readonly ConcurrentQueue<string> Logs = [];
    private string appName = string.Empty;

    public bool TryAcquire()
    {
        lock (lockObject)
        {
            if (isDisposed != 0 || inUse)
                return false;

            inUse = true;
            return true;
        }
    }

    public void Release()
    {
        lock (lockObject)
            inUse = false;
    }

    public ActionResponse Execute(ProgramConfig.Action action, ActionRequest request)
    {
        try
        {
            Logs.Clear();

            var response = ExecuteInternal(action, request);

            string[] capturedLogs = ProcessLogs(Logs);
            Logs.Clear();

            return response with { Logs = capturedLogs };
        }
        finally
        {
            Release();
        }
    }

    private static string[] ProcessLogs(ConcurrentQueue<string> rawLogs)
    {
        if (rawLogs.IsEmpty)
            return [];

        var processed = new List<string>();
        var currentStderrLines = new List<string>();

        foreach (string entry in rawLogs)
        {
            if (entry.StartsWith(Constants.OnActionStderr))
            {
                string line = entry[Constants.OnActionStderr.Length..].TrimStart();
                currentStderrLines.Add(line);
            }
            else
            {
                if (currentStderrLines.Count > 0)
                {
                    processed.Add($"{Constants.OnActionStderr} {string.Join("\n", currentStderrLines)}");
                    currentStderrLines.Clear();
                }
                processed.Add(entry);
            }
        }

        if (currentStderrLines.Count > 0)
            processed.Add($"{Constants.OnActionStderr} {string.Join("\n", currentStderrLines)}");

        return [.. processed];
    }

    private ActionResponse ExecuteInternal(ProgramConfig.Action action, ActionRequest request)
    {
        var errorResponse = StartProcess(request);
        if (errorResponse != null)
            return errorResponse;

        idleTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        ActionResponse? response = null;
        using var responseEvent = new AutoResetEvent(false);
        currentResponseEvent = responseEvent;
        bool actionTimedOut = false;

        actionTimer?.Dispose();
        actionTimer = new Timer(_ =>
        {
            actionTimedOut = true;
            responseEvent.Set();
        }, null, TimeSpan.FromSeconds(action.TimeoutInSeconds), Timeout.InfiniteTimeSpan);

        void outputHandler(object sender, DataReceivedEventArgs e) => HandleOutputData(e, request, responseEvent, ref response);

        process!.OutputDataReceived += outputHandler;

        string jsonPayload = System.Text.Json.JsonSerializer.Serialize(request, JsonDefaults.CompactOptions);
        string base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        process.StandardInput.WriteLine(base64Payload);

        responseEvent.WaitOne();

        try
        {
            process?.OutputDataReceived -= outputHandler;
        }
        catch { }

        currentResponseEvent = null;

        actionTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        if (actionTimedOut)
            return FailAndDispose(request, Constants.ActionTimeout, "A execução da ação excedeu o tempo limite.");

        if (response == null || process == null || process.HasExited)
            return FailAndDispose(request, Constants.ActionFailed, "O processo finalizou inesperadamente durante a execução da ação.");

        if (config.SlidingExpirationInSeconds > 0)
        {
            idleTimer?.Dispose();
            idleTimer = new Timer(_ => Dispose(), null, TimeSpan.FromSeconds(config.SlidingExpirationInSeconds), Timeout.InfiniteTimeSpan);
        }

        return response;
    }

    private void HandleOutputData(DataReceivedEventArgs e, ActionRequest request, AutoResetEvent responseEvent, ref ActionResponse? response)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        if (IsMessage(e.Data, Constants.OnActionLogged))
        {
            try
            {
                string logMessage = ExtractPayload(e.Data, Constants.OnActionLogged);
                Logs.Enqueue(logMessage);
            }
            catch (Exception ex)
            {
                Logs.Enqueue($"[Erro] Falha ao decodificar log: '{e.Data}' - {ex.Message}");
            }
            return;
        }

        try
        {
            if (IsMessage(e.Data, Constants.OnActionSuccess))
            {
                string decodedJsonPayload = ExtractPayload(e.Data, Constants.OnActionSuccess);
                var parsedResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(decodedJsonPayload, JsonDefaults.Options);
                response = new ActionResponse(
                    Previous: request,
                    Success: true,
                    Result: parsedResult,
                    AppName: appName
                );
                responseEvent.Set();
            }
            else if (IsMessage(e.Data, Constants.OnActionError))
            {
                string errorMessageText = ExtractPayload(e.Data, Constants.OnActionError);
                response = new ActionResponse(
                    Previous: request,
                    Success: false,
                    Error: new ActionError(Constants.ActionFailed, errorMessageText),
                    AppName: appName
                );
                responseEvent.Set();
            }
        }
        catch (Exception ex)
        {
            Logs.Enqueue($"[Erro] Falha ao processar mensagem do protocolo: '{e.Data}' - {ex.Message}");
            response = new ActionResponse(
                Previous: request,
                Success: false,
                Error: new ActionError(Constants.ActionInvalidResponse, $"Falha no protocolo: {ex.Message}"),
                AppName: appName
            );
            responseEvent.Set();
        }
    }

    private ActionResponse? StartProcess(ActionRequest request)
    {
        if (process != null)
            return null;

        using var startupEvent = new AutoResetEvent(false);
        currentStartupEvent = startupEvent;
        bool startupTimedOut = false;

        startupTimer?.Dispose();
        startupTimer = new Timer(_ =>
        {
            startupTimedOut = true;
            startupEvent.Set();
        }, null, TimeSpan.FromSeconds(config.StartupTimeoutInSeconds), Timeout.InfiniteTimeSpan);

        try
        {
            var (fileName, arguments, resolutionError) = ResolveExecutableInfo(config.Path);
            if (resolutionError != null)
                return FailAndDispose(request, Constants.StartupFailed, resolutionError);

            var utf8WithoutBom = new System.Text.UTF8Encoding(false);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StandardInputEncoding = utf8WithoutBom,
                StandardOutputEncoding = utf8WithoutBom,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, e) =>
            {
                currentStartupEvent?.Set();
                currentResponseEvent?.Set();
                Dispose();
            };

            string? startupErrorMessage = null;

            void startupOutputHandler(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                try
                {
                    if (IsMessage(e.Data, Constants.OnStartupSuccess))
                    {
                        appName = ExtractPayload(e.Data, Constants.OnStartupSuccess);
                        startupEvent.Set();
                    }
                    else if (IsMessage(e.Data, Constants.OnStartupError))
                    {
                        startupErrorMessage = ExtractPayload(e.Data, Constants.OnStartupError);
                        startupEvent.Set();
                    }
                }
                catch (Exception ex)
                {
                    startupErrorMessage = $"Falha ao decodificar mensagem de inicialização: {ex.Message}";
                    startupEvent.Set();
                }
            }

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logs.Enqueue($"{Constants.OnActionStderr} {e.Data}");
            };

            process.OutputDataReceived += startupOutputHandler;
            process.Start();
            ChildProcessTracker.AddProcess(process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            startupEvent.WaitOne();

            try
            {
                process?.OutputDataReceived -= startupOutputHandler;
            }
            catch { }

            currentStartupEvent = null;

            if (startupTimedOut)
                return FailAndDispose(request, Constants.StartupTimeout, "O processo excedeu o tempo limite de inicialização.");

            if (startupErrorMessage != null)
                return FailAndDispose(request, Constants.StartupFailed, startupErrorMessage);

            if (process == null || process.HasExited)
                return FailAndDispose(request, Constants.StartupFailed, "O processo finalizou inesperadamente durante a inicialização.");

            startupTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            return null;
        }
        catch (Exception ex)
        {
            return FailAndDispose(request, Constants.StartupFailed, ex.Message);
        }
    }

    private static (string fileName, string arguments, string? errorMessage) ResolveExecutableInfo(string path)
    {
        if (!File.Exists(path))
            return ("", "", $"O arquivo especificado em Path não foi encontrado: '{path}'.");

        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == Constants.ExeExtension)
            return ResolveExe(path);

        if (extension == Constants.PyExtension)
            return ResolvePython(path);

        return ("", "", $"Formato de arquivo '{extension}' não suportado. Apenas arquivos '{Constants.ExeExtension}' e '{Constants.PyExtension}' são permitidos.");
    }

    private static (string fileName, string arguments, string? errorMessage) ResolveExe(string path)
    {
        return (path, "", null);
    }

    private static (string fileName, string arguments, string? errorMessage) ResolvePython(string path)
    {
        string? scriptDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(scriptDir))
            scriptDir = Directory.GetCurrentDirectory();

        string venvPythonPath = Path.Combine(scriptDir, ".venv", "Scripts", $"python{Constants.ExeExtension}");
        string altVenvPythonPath = Path.Combine(scriptDir, "venv", "Scripts", $"python{Constants.ExeExtension}");

        string? selectedPython = null;
        if (File.Exists(venvPythonPath))
            selectedPython = venvPythonPath;
        else if (File.Exists(altVenvPythonPath))
            selectedPython = altVenvPythonPath;

        if (selectedPython == null)
            return ("", "", $"Ambiente virtual Python (.venv ou venv) não encontrado no diretório '{scriptDir}'.");

        return (selectedPython, $"\"{path}\"", null);
    }

    private static bool IsMessage(string? data, string prefixConstant)
    {
        return !string.IsNullOrEmpty(data) && data.StartsWith(prefixConstant);
    }

    private static string ExtractPayload(string? data, string prefixConstant)
    {
        if (string.IsNullOrEmpty(data) || !data.StartsWith(prefixConstant))
            throw new InvalidOperationException($"A mensagem não inicia com o prefixo esperado '{prefixConstant}'.");

        string rawBase64Payload = data[prefixConstant.Length..].Trim();
        if (string.IsNullOrWhiteSpace(rawBase64Payload))
            return string.Empty;

        byte[] bytes = Convert.FromBase64String(rawBase64Payload);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private ActionResponse FailAndDispose(ActionRequest request, string errorType, string errorMessage)
    {
        Dispose();
        return new ActionResponse(
            Previous: request,
            Success: false,
            Error: new ActionError(errorType, errorMessage),
            AppName: appName
        );
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) != 0)
            return;

        idleTimer?.Dispose();
        startupTimer?.Dispose();
        actionTimer?.Dispose();

        idleTimer = null;
        startupTimer = null;
        actionTimer = null;

        try
        {
            if (process is { HasExited: false })
                process.Kill(entireProcessTree: true);
        }
        catch { }

        try
        {
            process?.Dispose();
        }
        catch { }
        process = null;

        lock (lockObject)
            inUse = false;

        runner.RemoveInstance(this);
    }
}
