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

    public ActionResponse Execute(ProgramConfig.Handler handler, ActionRequest request)
    {
        lock (Logs)
        {
            Logs.Clear();
        }

        var response = ExecuteInternal(handler, request);

        string[] capturedLogs;
        lock (Logs)
        {
            capturedLogs = [.. Logs];
            Logs.Clear();
        }

        return response with { Logs = capturedLogs };
    }

    private ActionResponse ExecuteInternal(ProgramConfig.Handler handler, ActionRequest request)
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
        }, null, TimeSpan.FromSeconds(handler.TimeoutInSeconds), Timeout.InfiniteTimeSpan);

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
        if (e.Data == null)
            return;

        if (e.Data.StartsWith(Constants.OnActionLogged))
        {
            string rawLog = e.Data[Constants.OnActionLogged.Length..].Trim();
            string? logMessage = DecodeBase64(rawLog);
            if (logMessage != null)
            {
                lock (Logs)
                {
                    Logs.Add(logMessage);
                }
            }
        }
        else if (e.Data.StartsWith(Constants.OnActionSuccess))
        {
            string rawValue = e.Data[Constants.OnActionSuccess.Length..].Trim();
            string? decodedJson = DecodeBase64(rawValue);
            if (decodedJson == null)
            {
                Logs.Add($"[Erro] Valor retornado não é um Base64 válido: '{rawValue}'");

                response = new ActionResponse(
                    Previous: request,
                    Success: false,
                    Error: new ActionError(Constants.ActionInvalidResponse, "Falha ao decodificar a resposta base64 do processo.")
                );
            }
            else
            {
                try
                {
                    var parsedResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(decodedJson, JsonDefaults.Options);
                    response = new ActionResponse(
                        Previous: request,
                        Success: true,
                        Result: parsedResponse
                    );
                }
                catch (Exception ex)
                {
                    Logs.Add($"[Erro] JSON inválido recebido do processo: '{decodedJson}'");

                    response = new ActionResponse(
                        Previous: request,
                        Success: false,
                        Error: new ActionError(Constants.ActionInvalidResponse, $"Resposta do processo não é um JSON válido: {ex.Message}")
                    );
                }
            }
            responseEvent.Set();
        }
        else if (e.Data.StartsWith(Constants.OnActionError))
        {
            string rawMsg = e.Data[Constants.OnActionError.Length..].Trim();
            string? errorMsg = DecodeBase64(rawMsg);
            response = new ActionResponse(
                Previous: request,
                Success: false,
                Error: new ActionError(Constants.ActionFailed, errorMsg ?? string.Empty)
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
                if (e.Data == null)
                    return;

                if (e.Data.StartsWith(Constants.OnStartupSuccess))
                    startupEvent.Set();
                else if (e.Data.StartsWith(Constants.OnStartupError))
                {
                    startupErrorMessage = e.Data[Constants.OnStartupError.Length..].Trim();
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

    private static string? DecodeBase64(string rawBase64)
    {
        if (string.IsNullOrWhiteSpace(rawBase64))
            return null;

        try
        {
            byte[] bytes = Convert.FromBase64String(rawBase64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return rawBase64;
        }
    }

    private ActionResponse FailAndDispose(ActionRequest request, string errorType, string errorMessage)
    {
        Dispose();
        return new ActionResponse(
            Previous: request,
            Success: false,
            Error: new ActionError(errorType, errorMessage)
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

