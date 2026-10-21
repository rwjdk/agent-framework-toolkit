using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A C# client for the Codex app-server JSON protocol over standard input and output.</summary>
[PublicAPI]
public sealed class CodexClient : IAsyncDisposable
{
    private readonly CodexConnection _connection;
    private readonly SemaphoreSlim _startup = new(1, 1);
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly Channel<JsonElement> _events = Channel.CreateUnbounded<JsonElement>();
    private TextReader? _output;
    private TextWriter? _input;
    private Process? _process;
    private Task? _readerTask;
    private Task? _errorTask;
    private Exception? _failure;
    private bool _initialized;
    private bool _requiresExperimentalApi;
    private bool _experimentalApiEnabled;
    private int _nextId;
    private int _disposed;

    internal SemaphoreSlim RunGate { get; } = new(1, 1);

    /// <summary>Creates a client. No process is started until the first request.</summary>
    /// <param name="connection">Optional runtime configuration.</param>
    public CodexClient(CodexConnection? connection = null)
    {
        _connection = connection ?? new CodexConnection();
        if (_connection.RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(connection), "RequestTimeout must be positive.");
        }
    }

    internal CodexClient(TextReader output, TextWriter input) : this()
    {
        _output = output;
        _input = input;
    }

    /// <summary>Responds to server-initiated approvals, questions, and tool calls using typed request helpers.</summary>
    public Func<CodexRunRequest, CancellationToken, Task<CodexRunResponse>>? OnRunResponseRequired { get; set; }

    internal void RequireExperimentalApi()
    {
        if (_readerTask is not null && !_experimentalApiEnabled)
        {
            throw new InvalidOperationException("Register tools before starting the client, or initialize the client with capabilities.experimentalApi enabled.");
        }
        _requiresExperimentalApi = true;
    }

    /// <summary>Starts the process and completes the protocol initialization handshake.</summary>
    /// <param name="cancellationToken">Cancels startup.</param>
    /// <returns>A task representing initialization.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        await _startup.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfUnavailable();
            if (_initialized)
            {
                return;
            }

            if (_input is null)
            {
                ProcessStartInfo startInfo = CreateStartInfo();
                _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Codex app-server.");
                _input = _process.StandardInput;
                _output = _process.StandardOutput;
                _errorTask = ReadStandardErrorAsync(_process.StandardError);
            }
            _readerTask = ReadMessagesAsync();
            JsonObject parameters = new()
            {
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = _connection.ClientName,
                    ["title"] = "Agent Framework Toolkit",
                    ["version"] = typeof(CodexClient).Assembly.GetName().Version?.ToString() ?? "1.0.0"
                }
            };
            Merge(parameters, _connection.InitializeParameters);
            if (_requiresExperimentalApi)
            {
                JsonObject capabilities = parameters["capabilities"] as JsonObject ?? new JsonObject();
                capabilities["experimentalApi"] = true;
                parameters["capabilities"] = capabilities;
            }
            _experimentalApiEnabled = parameters["capabilities"] is JsonObject enabledCapabilities &&
                enabledCapabilities["experimentalApi"]?.GetValue<bool>() == true;
            await RequestCoreAsync("initialize", parameters, cancellationToken).ConfigureAwait(false);
            await WriteAsync(new JsonObject { ["method"] = "initialized" }, cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        catch (Exception error)
        {
            Fail(error);
            StopProcess();
            throw;
        }
        finally
        {
            _startup.Release();
        }
    }

    /// <summary>Sends a native request, including account, model, thread, and configuration operations.</summary>
    /// <param name="method">The native method name.</param>
    /// <param name="parameters">The native request parameters.</param>
    /// <param name="cancellationToken">Cancels waiting for the response.</param>
    /// <returns>The native result. Protocol errors throw an exception.</returns>
    public async Task<JsonElement> RequestAsync(string method, JsonObject? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        await StartAsync(cancellationToken).ConfigureAwait(false);
        return await RequestCoreAsync(method, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal IAsyncEnumerable<JsonElement> ReadEventsAsync(CancellationToken cancellationToken)
    {
        return _events.Reader.ReadAllAsync(cancellationToken);
    }

    internal static void Merge(JsonObject target, JsonObject source)
    {
        foreach (KeyValuePair<string, JsonNode?> entry in source)
        {
            target[entry.Key] = entry.Value?.DeepClone();
        }
    }

    internal static JsonNode DefaultServerRequestResult(string method)
    {
        return method is "item/commandExecution/requestApproval" or "item/fileChange/requestApproval"
            ? new JsonObject { ["decision"] = "accept" }
            : throw new NotSupportedException("Configure OnRunResponseRequired to handle " + method);
    }

    private async Task<JsonElement> RequestCoreAsync(string method, JsonObject? parameters, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        int id = Interlocked.Increment(ref _nextId);
        string key = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        TaskCompletionSource<JsonElement> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[key] = completion;
        try
        {
            // Recheck after registration so a concurrent EOF cannot leave this request pending.
            ThrowIfUnavailable();
            await WriteAsync(new JsonObject
            {
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters?.DeepClone() ?? new JsonObject()
            }, cancellationToken).ConfigureAwait(false);
            return await completion.Task.WaitAsync(_connection.RequestTimeout, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pending.TryRemove(key, out _);
        }
    }

    private async Task WriteAsync(JsonObject message, CancellationToken cancellationToken)
    {
        await _writer.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfUnavailable();
            await _input!.WriteLineAsync(message.ToJsonString().AsMemory(), cancellationToken).ConfigureAwait(false);
            await _input.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writer.Release();
        }
    }

    private async Task ReadMessagesAsync()
    {
        try
        {
            while (await _output!.ReadLineAsync(_lifetime.Token).ConfigureAwait(false) is string line)
            {
                using JsonDocument document = JsonDocument.Parse(line);
                JsonElement message = document.RootElement.Clone();
                if (message.TryGetProperty("method", out JsonElement method))
                {
                    if (message.TryGetProperty("id", out _))
                    {
                        _ = HandleServerRequestAsync(message, method.GetString()!);
                    }
                    else
                    {
                        _events.Writer.TryWrite(message);
                    }
                }
                else if (message.TryGetProperty("id", out JsonElement id) && _pending.TryRemove(id.GetRawText(), out TaskCompletionSource<JsonElement>? completion))
                {
                    if (message.TryGetProperty("error", out JsonElement error))
                    {
                        completion.TrySetException(new InvalidOperationException("Codex protocol error: " + error.GetRawText()));
                    }
                    else
                    {
                        if (message.TryGetProperty("result", out JsonElement result))
                        {
                            completion.TrySetResult(result.Clone());
                        }
                        else
                        {
                            completion.TrySetException(new IOException("Codex returned a response without a result or error."));
                        }
                    }
                }
            }
            Fail(new IOException("Codex app-server closed its output stream."));
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private async Task HandleServerRequestAsync(JsonElement message, string method)
    {
        JsonObject response = new() { ["id"] = JsonNode.Parse(message.GetProperty("id").GetRawText()) };
        try
        {
            JsonElement parameters = message.GetProperty("params");
            if (OnRunResponseRequired is not null)
            {
                CodexRunRequest request = new(method, parameters);
                CodexRunResponse result = await OnRunResponseRequired(request, _lifetime.Token).ConfigureAwait(false);
                response["result"] = request.GetResult(result);
            }
            else
            {
                response["result"] = DefaultServerRequestResult(method);
            }
        }
        catch (NotSupportedException error)
        {
            response["error"] = new JsonObject { ["code"] = -32601, ["message"] = error.Message };
        }
        catch (Exception error)
        {
            response["error"] = new JsonObject { ["code"] = -32603, ["message"] = error.Message };
        }
        try
        {
            await WriteAsync(response, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private async Task ReadStandardErrorAsync(StreamReader reader)
    {
        try
        {
            while (await reader.ReadLineAsync(_lifetime.Token).ConfigureAwait(false) is string line)
            {
                _connection.OnStandardError?.Invoke(line);
            }
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private ProcessStartInfo CreateStartInfo()
    {
        ProcessStartInfo info = new(ResolveExecutable(_connection.ExecutablePath))
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in _connection.Arguments)
        {
            info.ArgumentList.Add(argument);
        }
        if (_connection.CodexHome is not null)
        {
            info.Environment["CODEX_HOME"] = _connection.CodexHome;
        }
        foreach (KeyValuePair<string, string?> entry in _connection.Environment)
        {
            info.Environment[entry.Key] = entry.Value;
        }
        if (_connection.ApiKey is not null)
        {
            // Keep the API key out of process arguments and the user's persistent login.
            info.Environment["AFT_CODEX_API_KEY"] = _connection.ApiKey;
            string[] settings =
            [
                "model_provider=\"aft_openai\"",
                "model_providers.aft_openai.name=\"AFT OpenAI\"",
                "model_providers.aft_openai.base_url=" + JsonSerializer.Serialize(_connection.Endpoint),
                "model_providers.aft_openai.env_key=\"AFT_CODEX_API_KEY\"",
                "model_providers.aft_openai.wire_api=\"responses\"",
                "model_providers.aft_openai.requires_openai_auth=false"
            ];
            foreach (string setting in settings)
            {
                info.ArgumentList.Add("-c");
                info.ArgumentList.Add(setting);
            }
        }
        info.ArgumentList.Add("app-server");
        return info;
    }

    private static string ResolveExecutable(string executable)
    {
        if (!OperatingSystem.IsWindows() || executable != "codex")
        {
            return executable;
        }
        string architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        string target = architecture == "arm64" ? "aarch64" : "x86_64";
        foreach (string directory in (System.Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string root = directory.Trim('"');
            string[] candidates =
            [
                Path.Combine(root, "codex.exe"),
                Path.Combine(root, "node_modules", "@openai", "codex", "node_modules", "@openai", "codex-win32-" + architecture,
                    "vendor", target + "-pc-windows-msvc", "bin", "codex.exe")
            ];
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        throw new FileNotFoundException("Could not locate codex.exe. Set CodexConnection.ExecutablePath to the native executable.");
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (_failure is Exception failure)
        {
            throw new IOException("The Codex connection is no longer available. Create a new client to resume its threads.", failure);
        }
    }

    private void Fail(Exception error)
    {
        Interlocked.CompareExchange(ref _failure, error, null);
        foreach (KeyValuePair<string, TaskCompletionSource<JsonElement>> entry in _pending)
        {
            if (_pending.TryRemove(entry.Key, out TaskCompletionSource<JsonElement>? completion))
            {
                completion.TrySetException(error);
            }
        }
        _events.Writer.TryComplete(error);
    }

    private void StopProcess()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The child may exit between the state check and Kill.
        }
    }

    /// <summary>Stops the app-server and faults any pending requests.</summary>
    /// <returns>A task representing shutdown.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        Fail(new ObjectDisposedException(nameof(CodexClient)));
        List<Exception> cleanupErrors = [];
        try
        {
            await _lifetime.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            cleanupErrors.Add(error);
        }
        try
        {
            StopProcess();
        }
        catch (Exception error)
        {
            cleanupErrors.Add(error);
        }
        try
        {
            await Task.WhenAll(_readerTask ?? Task.CompletedTask, _errorTask ?? Task.CompletedTask).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            cleanupErrors.Add(error);
        }
        DisposeResource(_input, cleanupErrors);
        DisposeResource(_output, cleanupErrors);
        DisposeResource(_process, cleanupErrors);
        DisposeResource(_lifetime, cleanupErrors);
        if (cleanupErrors.Count > 0)
        {
            throw new AggregateException("Codex client cleanup failed.", cleanupErrors);
        }
    }

    private static void DisposeResource(IDisposable? resource, List<Exception> errors)
    {
        try
        {
            resource?.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
    }
}
