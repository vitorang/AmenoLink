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
    private List<string> Logs = new();

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
        var errorResponse = StartProccess(handler, request);
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

        DataReceivedEventHandler outputHandler = (sender, e) =>
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
                string? responseValue = DecodeBase64(rawValue);
                response = new ActionResponse(
                    ActionRequest: request,
                    Success: true,
                    Response: responseValue
                );
                responseEvent.Set();
            }
            else if (e.Data.StartsWith(Constants.OnActionError))
            {
                string rawMsg = e.Data[Constants.OnActionError.Length..].Trim();
                string? errorMsg = DecodeBase64(rawMsg);
                response = new ActionResponse(
                    ActionRequest: request,
                    Success: false,
                    ErrorType: Constants.ActionFailed,
                    ErrorMessage: errorMsg
                );
                responseEvent.Set();
            }
        };

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

    private ActionResponse? StartProccess(ProgramConfig.Handler handler, ActionRequest request)
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

            DataReceivedEventHandler startupOutputHandler = (sender, e) =>
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
            };

            proccess.OutputDataReceived += startupOutputHandler;
            proccess.Start();
            proccess.BeginOutputReadLine();

            startupEvent.WaitOne();

            proccess.OutputDataReceived -= startupOutputHandler;
            currentStartupEvent = null;

            if (startupTimedOut)
                return FailAndDispose(request, Constants.StartupTimeout, "O processo excedeu o tempo limite de inicialização.");

            if (startupErrorMessage != null)
                return FailAndDispose(request, Constants.StartupFailed, startupErrorMessage);

            startupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            inUse = false;

            return null;
        }
        catch (Exception ex)
        {
            return FailAndDispose(request, Constants.StartupFailed, ex.Message);
        }
    }

    private (string fileName, string arguments, string? errorMessage) ResolveExecutableInfo(string path)
    {
        if (!File.Exists(path))
            return ("", "", $"O arquivo especificado em Path não foi encontrado: '{path}'.");

        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".exe")
            return (path, "", null);

        if (extension == ".py")
        {
            string? scriptDir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(scriptDir))
                scriptDir = Directory.GetCurrentDirectory();

            string venvPythonPath = Path.Combine(scriptDir, ".venv", "Scripts", "python.exe");
            string altVenvPythonPath = Path.Combine(scriptDir, "venv", "Scripts", "python.exe");

            string? selectedPython = null;
            if (File.Exists(venvPythonPath))
                selectedPython = venvPythonPath;
            else if (File.Exists(altVenvPythonPath))
                selectedPython = altVenvPythonPath;

            if (selectedPython == null)
                return ("", "", $"Ambiente virtual Python (.venv ou venv) não encontrado no diretório '{scriptDir}'.");

            return (selectedPython, $"\"{path}\"", null);
        }

        return ("", "", $"Formato de arquivo '{extension}' não suportado. Apenas arquivos '.exe' e '.py' são permitidos.");
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
            ActionRequest: request,
            Success: false,
            ErrorType: errorType,
            ErrorMessage: errorMessage
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

