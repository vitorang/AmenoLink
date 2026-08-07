using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.Shared;
using System.Diagnostics;
using Timer = System.Threading.Timer;

namespace AmenoLink.ProgramManager;

internal sealed class ProcessInstance(IProgramRunner runner, ProgramConfig config) : IDisposable
{
    public bool InUse => inUse;
    private bool inUse = false;
    private Process? proccess;
    private Timer? idleTimer;
    private Timer? startupTimer;
    private Timer? actionTimer;
    private AutoResetEvent? currentResponseEvent;
    private AutoResetEvent? currentStartupEvent;
    private readonly List<string> Logs = [];
    private string appName = string.Empty;

    public ActionResponse Execute(ProgramConfig.Action action, ActionRequest request)
    {
        Logs.Clear();

        var response = ExecuteInternal(action, request);

        string[] capturedLogs = [.. Logs];
        Logs.Clear();

        return response with { Logs = capturedLogs };
    }

    private ActionResponse ExecuteInternal(ProgramConfig.Action action, ActionRequest request)
    {
        var errorResponse = StartProccess(request);
        if (errorResponse != null)
            return errorResponse;

        inUse = true;

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

        proccess!.OutputDataReceived += outputHandler;

        string jsonPayload = System.Text.Json.JsonSerializer.Serialize(request, JsonDefaults.CompactOptions);
        string base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        proccess.StandardInput.WriteLine(base64Payload);

        responseEvent.WaitOne();

        proccess.OutputDataReceived -= outputHandler;
        currentResponseEvent = null;

        actionTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        if (actionTimedOut || response == null)
            return FailAndDispose(request, Constants.ActionTimeout, "A execução da ação excedeu o tempo limite.");

        if (config.SlidingExpirationInSeconds > 0)
        {
            idleTimer?.Dispose();
            idleTimer = new Timer(_ => Dispose(), null, TimeSpan.FromSeconds(config.SlidingExpirationInSeconds), Timeout.InfiniteTimeSpan);
        }

        inUse = false;

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
                Logs.Add(logMessage);
            }
            catch (Exception ex)
            {
                Logs.Add($"[Erro] Falha ao decodificar log: '{e.Data}' - {ex.Message}");
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
            Logs.Add($"[Erro] Falha ao processar mensagem do protocolo: '{e.Data}' - {ex.Message}");
            response = new ActionResponse(
                Previous: request,
                Success: false,
                Error: new ActionError(Constants.ActionInvalidResponse, $"Falha no protocolo: {ex.Message}"),
                AppName: appName
            );
            responseEvent.Set();
        }
    }

    private ActionResponse? StartProccess(ActionRequest request)
    {
        if (proccess != null)
            return null;

        inUse = true;

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

            proccess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            proccess.Exited += (sender, e) =>
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

            proccess.OutputDataReceived += startupOutputHandler;
            proccess.Start();
            ChildProcessTracker.AddProcess(proccess);
            proccess.BeginOutputReadLine();

            startupEvent.WaitOne();

            proccess.OutputDataReceived -= startupOutputHandler;
            currentStartupEvent = null;

            if (startupTimedOut)
                return FailAndDispose(request, Constants.StartupTimeout, "O processo excedeu o tempo limite de inicialização.");

            if (startupErrorMessage != null)
                return FailAndDispose(request, Constants.StartupFailed, startupErrorMessage);

            if (proccess.HasExited)
                return FailAndDispose(request, Constants.StartupFailed, "O processo finalizou inesperadamente durante a inicialização.");

            startupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            inUse = false;

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
            return (path, "", null);

        if (extension == Constants.PyExtension)
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

        return ("", "", $"Formato de arquivo '{extension}' não suportado. Apenas arquivos '{Constants.ExeExtension}' e '{Constants.PyExtension}' são permitidos.");
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
        idleTimer?.Dispose();
        startupTimer?.Dispose();
        actionTimer?.Dispose();

        idleTimer = null;
        startupTimer = null;
        actionTimer = null;

        try
        {
            if (proccess is { HasExited: false })
                proccess.Kill(entireProcessTree: true);
        }
        catch { }

        proccess?.Dispose();
        proccess = null;

        inUse = false;
        runner.RemoveInstance(this);
    }
}
