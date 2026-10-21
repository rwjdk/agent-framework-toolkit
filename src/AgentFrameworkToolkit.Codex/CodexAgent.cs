using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;

namespace AgentFrameworkToolkit.Codex;

/// <summary>An Agent Framework agent controlling a local Codex app-server.</summary>
[PublicAPI]
public sealed class CodexAgent : AIAgent, IAsyncDisposable
{
    private readonly CodexAgentOptions _options;
    private readonly bool _ownsClient;
    private readonly CodexFunctionTools? _tools;
    private ToolInvocationScope? _toolScope;
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _disposeLock = new();
    private Task? _disposeTask;
    private readonly ConcurrentDictionary<string, PendingApproval> _approvals = new();
    private IAsyncEnumerator<AgentResponseUpdate>? _pendingRun;
    private CodexAgentSession? _pendingSession;
    private bool _implicitSession;
    private bool _streamActive;
    private CancellationTokenSource? _runLifetime;
    private volatile bool _disposed;

    /// <summary>Creates an agent using an existing Codex client.</summary>
    /// <param name="client">A client dedicated to this agent's event stream.</param>
    /// <param name="options">Thread and agent settings, including the required working directory.</param>
    /// <param name="ownsClient">Whether disposing this agent also disposes the client.</param>
    internal CodexAgent(CodexClient client, CodexAgentOptions options, bool ownsClient = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WorkingDirectory);
        Client = client;
        _options = options;
        _ownsClient = ownsClient;
        if (options.Tools is { Count: > 0 })
        {
            _tools = new CodexFunctionTools(options.Tools, options.Services);
            Client.RequireExperimentalApi();
        }
        Client.OnServerRequest = HandleServerRequestAsync;
    }

    internal CodexClient Client { get; }

    /// <inheritdoc />
    protected override string? IdCore => _options.Id;

    /// <inheritdoc />
    public override string? Name => _options.Name;

    /// <inheritdoc />
    public override string? Description => _options.Description;

    /// <summary>Creates a session that resumes a persisted Codex thread on its next run.</summary>
    /// <param name="threadId">The saved native thread identifier.</param>
    /// <returns>A resumable session.</returns>
    public ValueTask<AgentSession> CreateSessionAsync(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        return new ValueTask<AgentSession>(new CodexAgentSession { ThreadId = threadId });
    }

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<AgentSession>(new CodexAgentSession());
    }

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CodexAgentSession typedSession = GetSession(session);
        if (ReferenceEquals(typedSession, _pendingSession))
        {
            throw new InvalidOperationException("Complete the pending run before serializing its session. Pending approvals belong to this agent instance.");
        }
        JsonObject state = new()
        {
            ["threadId"] = typedSession.ThreadId,
            ["stateBag"] = JsonNode.Parse(typedSession.StateBag.Serialize().GetRawText())
        };
        return new ValueTask<JsonElement>(JsonSerializer.SerializeToElement(state, jsonSerializerOptions));
    }

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedSession, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? threadId = serializedSession.GetProperty("threadId").GetString();
        AgentSessionStateBag stateBag = AgentSessionStateBag.Deserialize(serializedSession.GetProperty("stateBag"));
        return new ValueTask<AgentSession>(new CodexAgentSession(stateBag) { ThreadId = threadId });
    }

    /// <inheritdoc />
    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RunUpdatesAsync(messages, session, options, false, cancellationToken).ToAgentResponseAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunUpdatesAsync(messages, session, options, true, cancellationToken);
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunUpdatesAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, bool isStreaming,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        IList<ChatMessage> input = messages.ToList();
        await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        bool paused = false;
        bool ownsRun = false;
        bool ownsStream = false;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_streamActive)
            {
                throw new InvalidOperationException("Finish or dispose the active response stream before starting another run.");
            }
            _streamActive = true;
            ownsStream = true;
            if (_pendingRun is not null)
            {
                if (!ReferenceEquals(session, _pendingSession) && !(session is null && _implicitSession))
                {
                    throw new InvalidOperationException("Continue approvals using the same session and agent instance.");
                }
                List<ToolApprovalResponseContent> responses = input.SelectMany(message => message.Contents).OfType<ToolApprovalResponseContent>().ToList();
                if (responses.Count == 0 || input.Any(message => message.Role != ChatRole.User || message.Contents.Any(content => content is not ToolApprovalResponseContent)) ||
                    responses.Select(response => response.RequestId).Distinct(StringComparer.Ordinal).Count() != responses.Count ||
                    responses.Any(response => !_approvals.ContainsKey(response.RequestId)))
                {
                    throw new ArgumentException("Supply only responses to this session's pending approval requests.", nameof(messages));
                }
                ownsRun = true;
                foreach (ToolApprovalResponseContent response in responses)
                {
                    if (_approvals.TryRemove(response.RequestId, out PendingApproval? approval))
                    {
                        approval.Decision.TrySetResult(response.Approved);
                    }
                }
            }
            else
            {
                if (input.SelectMany(message => message.Contents).Any(content => content is ToolApprovalResponseContent))
                {
                    throw new ArgumentException("There is no pending approval for this agent.", nameof(messages));
                }
                _pendingSession = session is null ? new CodexAgentSession() : GetSession(session);
                _implicitSession = session is null;
                _runLifetime = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
                _pendingRun = RunNativeUpdatesAsync(input, _pendingSession, options, isStreaming, _runLifetime.Token).GetAsyncEnumerator();
                ownsRun = true;
            }
            CancellationTokenSource runLifetime = _runLifetime!;
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                try
                {
                    runLifetime.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Agent disposal can finish while the consumer is between streamed updates.
                }
            });
            while (await _pendingRun.MoveNextAsync().ConfigureAwait(false))
            {
                AgentResponseUpdate update = _pendingRun.Current;
                if (update.Contents.OfType<ToolApprovalRequestContent>().Any())
                {
                    paused = true;
                }
                // Let agent disposal acquire the lock while the consumer processes an update.
                _runLock.Release();
                try
                {
                    yield return update;
                }
                finally
                {
                    await _runLock.WaitAsync().ConfigureAwait(false);
                }
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (paused)
                {
                    yield break;
                }
            }
        }
        finally
        {
            try
            {
                if (ownsRun && (!paused || cancellationToken.IsCancellationRequested))
                {
                    await ClearRunAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (ownsStream)
                {
                    _streamActive = false;
                }
                _runLock.Release();
            }
        }
    }

    private async ValueTask ClearRunAsync()
    {
        try
        {
            if (_pendingRun is not null)
            {
                await _pendingRun.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            foreach (PendingApproval approval in _approvals.Values)
            {
                approval.Decision.TrySetCanceled();
            }
            _approvals.Clear();
            _pendingRun = null;
            _pendingSession = null;
            _implicitSession = false;
            _runLifetime?.Dispose();
            _runLifetime = null;
        }
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunNativeUpdatesAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, bool isStreaming,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
#pragma warning disable MEAI001 // Explicitly reject unsupported background response continuation.
        if (options?.ContinuationToken is not null)
        {
            throw new NotSupportedException("Codex uses native thread sessions rather than background response continuation tokens.");
        }
#pragma warning restore MEAI001
        CodexAgentSession typedSession = session is null ? new CodexAgentSession() : GetSession(session);
        IList<ChatMessage> inputMessages = messages.ToList();
        JsonArray input = CreateInput(inputMessages);
        await Client.RunGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        using CancellationTokenSource toolLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        string? turnId = null;
        bool turnRequested = false;
        bool completed = false;
        try
        {
            JsonObject threadParameters = CreateThreadParameters(typedSession.ThreadId is not null);
            if (typedSession.ThreadId is not null)
            {
                threadParameters["threadId"] = typedSession.ThreadId;
            }
            JsonElement threadResult = await Client.RequestAsync(
                typedSession.ThreadId is null ? "thread/start" : "thread/resume", threadParameters, cancellationToken).ConfigureAwait(false);
            typedSession.ThreadId = threadResult.GetProperty("thread").GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Codex did not return a thread identifier.");
            _toolScope = new ToolInvocationScope(typedSession.ThreadId, inputMessages, isStreaming, toolLifetime.Token);

            JsonObject turnParameters = new()
            {
                ["input"] = input
            };
            Set(turnParameters, "effort", _options.ReasoningEffort switch
            {
                null => null,
                CodexReasoningEffort.None => "none",
                CodexReasoningEffort.Minimal => "minimal",
                CodexReasoningEffort.Low => "low",
                CodexReasoningEffort.Medium => "medium",
                CodexReasoningEffort.High => "high",
                CodexReasoningEffort.ExtraHigh => "xhigh",
                CodexReasoningEffort.Max => "max",
                _ => throw new ArgumentOutOfRangeException(nameof(CodexAgentOptions.ReasoningEffort))
            });
            Set(turnParameters, "summary", _options.ReasoningSummary switch
            {
                null => null,
                CodexReasoningSummary.Auto => "auto",
                CodexReasoningSummary.Concise => "concise",
                CodexReasoningSummary.Detailed => "detailed",
                CodexReasoningSummary.None => "none",
                _ => throw new ArgumentOutOfRangeException(nameof(CodexAgentOptions.ReasoningSummary))
            });
            if (_options.OutputSchema is not null)
            {
                turnParameters["outputSchema"] = _options.OutputSchema.DeepClone();
            }
            if (options?.ResponseFormat is ChatResponseFormatJson { Schema: JsonElement schema })
            {
                JsonElement normalizedSchema = StructuredOutputSchemaHelper.NormalizeObjectSchemas(schema);
                turnParameters["outputSchema"] = JsonNode.Parse(normalizedSchema.GetRawText());
            }
            else if (options?.ResponseFormat is ChatResponseFormatText)
            {
                turnParameters.Remove("outputSchema");
            }
            CodexClient.Merge(turnParameters, _options.TurnStartParameters);
            if (options is CodexAgentRunOptions codexOptions)
            {
                CodexClient.Merge(turnParameters, codexOptions.Parameters);
            }
            turnParameters["threadId"] = typedSession.ThreadId;
            turnRequested = true;
            JsonElement turnResult = await Client.RequestAsync("turn/start", turnParameters, cancellationToken).ConfigureAwait(false);
            turnId = turnResult.GetProperty("turn").GetProperty("id").GetString();
            _toolScope.TurnId = turnId;
            HashSet<string> streamedMessages = [];
            HashSet<string> completedFileChanges = [];
            List<CodexFileChange> fileChanges = [];
            Dictionary<string, string?> messagePhases = [];
            bool structuredOutput = turnParameters["outputSchema"] is not null;
            JsonElement? finalMessage = null;
            JsonElement? unphasedMessage = null;
            UsageDetails? usage = null;
            UsageDetails? usageBaseline = null;

            await foreach (JsonElement message in Client.ReadEventsAsync(cancellationToken).ConfigureAwait(false))
            {
                string method = message.GetProperty("method").GetString()!;
                message.TryGetProperty("params", out JsonElement parameters);
                if (method == "aft/approval")
                {
                    string requestId = parameters.GetProperty("requestId").GetString()!;
                    if (_approvals.TryGetValue(requestId, out PendingApproval? approval))
                    {
                        yield return new AgentResponseUpdate
                        {
                            Role = ChatRole.Assistant,
                            ResponseId = turnId,
                            MessageId = requestId,
                            Contents = [approval.Content]
                        };
                    }
                    continue;
                }
                if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("threadId", out JsonElement eventThread) &&
                    eventThread.ValueKind == JsonValueKind.String && eventThread.GetString() != typedSession.ThreadId)
                {
                    continue;
                }
                if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("turnId", out JsonElement eventTurn) &&
                    eventTurn.ValueKind == JsonValueKind.String && eventTurn.GetString() != turnId)
                {
                    continue;
                }
                if (method is "turn/started" or "turn/completed" &&
                    parameters.GetProperty("turn").GetProperty("id").GetString() != turnId)
                {
                    continue;
                }
                if (_options.OnRunEvent is not null && CodexRunEvent.FromEvent(message) is { } runEvent)
                {
                    _options.OnRunEvent(runEvent);
                }
                AgentResponseUpdate update = new()
                {
                    Role = ChatRole.Assistant,
                    ResponseId = turnId,
                    RawRepresentation = message
                };
                if (method == "item/started")
                {
                    JsonElement item = parameters.GetProperty("item");
                    if (item.GetProperty("type").GetString() == "agentMessage")
                    {
                        messagePhases[item.GetProperty("id").GetString()!] = GetMessagePhase(item);
                    }
                }
                else if (method == "item/agentMessage/delta")
                {
                    string itemId = parameters.GetProperty("itemId").GetString()!;
                    // Unknown phases are resolved by item/completed before exposing response text.
                    if (!structuredOutput && messagePhases.TryGetValue(itemId, out string? phase) && phase is "final_answer" or "partial_answer")
                    {
                        streamedMessages.Add(itemId);
                        update.MessageId = itemId;
                        update.Contents = [new TextContent(parameters.GetProperty("delta").GetString()!)];
                    }
                }
                else if (method == "item/completed")
                {
                    JsonElement item = parameters.GetProperty("item");
                    if (item.GetProperty("type").GetString() == "fileChange" &&
                        item.GetProperty("status").GetString() == "completed" &&
                        completedFileChanges.Add(item.GetProperty("id").GetString()!))
                    {
                        fileChanges.AddRange(CodexFileChange.FromItem(item));
                    }
                    if (item.GetProperty("type").GetString() == "agentMessage")
                    {
                        string itemId = item.GetProperty("id").GetString()!;
                        string? phase = GetMessagePhase(item);
                        if (structuredOutput)
                        {
                            // Typed results must contain one final JSON document, never preambles or partial answers.
                            if (phase == "final_answer")
                            {
                                finalMessage = item;
                            }
                            else if (phase is null)
                            {
                                unphasedMessage = item;
                            }
                        }
                        else if (phase != "commentary" && !streamedMessages.Contains(itemId))
                        {
                            update.MessageId = itemId;
                            update.Contents = [new TextContent(item.GetProperty("text").GetString()!)];
                            streamedMessages.Add(itemId);
                        }
                    }
                }
                else if (method == "thread/tokenUsage/updated")
                {
                    JsonElement last = parameters.GetProperty("tokenUsage").GetProperty("last");
                    JsonElement total = parameters.GetProperty("tokenUsage").GetProperty("total");
                    usageBaseline ??= new UsageDetails
                    {
                        InputTokenCount = total.GetProperty("inputTokens").GetInt64() - last.GetProperty("inputTokens").GetInt64(),
                        OutputTokenCount = total.GetProperty("outputTokens").GetInt64() - last.GetProperty("outputTokens").GetInt64(),
                        TotalTokenCount = total.GetProperty("totalTokens").GetInt64() - last.GetProperty("totalTokens").GetInt64()
                    };
                    usage = new UsageDetails
                    {
                        InputTokenCount = total.GetProperty("inputTokens").GetInt64() - usageBaseline.InputTokenCount,
                        OutputTokenCount = total.GetProperty("outputTokens").GetInt64() - usageBaseline.OutputTokenCount,
                        TotalTokenCount = total.GetProperty("totalTokens").GetInt64() - usageBaseline.TotalTokenCount
                    };
                }
                else if (method == "turn/completed")
                {
                    JsonElement turn = parameters.GetProperty("turn");
                    if (turn.GetProperty("id").GetString() != turnId)
                    {
                        continue;
                    }
                    completed = true;
                    string? status = turn.GetProperty("status").GetString();
                    if (status != "completed")
                    {
                        throw new InvalidOperationException("Codex turn " + status + ": " +
                            (turn.TryGetProperty("error", out JsonElement error) ? error.GetRawText() : "No error details."));
                    }
                    if (structuredOutput)
                    {
                        JsonElement answer = finalMessage ?? unphasedMessage
                            ?? throw new InvalidOperationException("Codex completed without a final structured response.");
                        update.MessageId = answer.GetProperty("id").GetString();
                        update.Contents = [new TextContent(answer.GetProperty("text").GetString()!)];
                    }
                    if (usage is not null)
                    {
                        update.Contents.Add(new UsageContent(usage));
                    }
                    update.AdditionalProperties = new()
                    {
                        [CodexAgentResponseExtensions.FileChangesKey] = fileChanges.AsReadOnly()
                    };
                    yield return update;
                    yield break;
                }
                yield return update;
            }
            throw new IOException("Codex stopped before completing the turn.");
        }
        finally
        {
            _toolScope = null;
            try
            {
                try
                {
                    await toolLifetime.CancelAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (turnRequested && !completed)
                    {
                        try
                        {
                            if (turnId is not null)
                            {
                                using CancellationTokenSource interruptTimeout = new(TimeSpan.FromSeconds(5));
                                await Client.RequestAsync("turn/interrupt", new JsonObject
                                {
                                    ["threadId"] = typedSession.ThreadId,
                                    ["turnId"] = turnId
                                }, interruptTimeout.Token).ConfigureAwait(false);
                            }
                        }
                        catch (Exception)
                        {
                            // Closing the connection below also stops the owned runtime after a failed interrupt.
                        }
                        finally
                        {
                            // An abandoned stream must not leave Codex executing unseen in the background.
                            await Client.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                Client.RunGate.Release();
            }
        }
    }

    private JsonObject CreateThreadParameters(bool resume)
    {
        JsonObject parameters = new();
        Set(parameters, "model", _options.Model);
        Set(parameters, "cwd", _options.WorkingDirectory);
        Set(parameters, "developerInstructions", _options.Instructions);
        Set(parameters, "modelProvider", _options.ModelProvider);
        Set(parameters, "serviceTier", _options.ServiceTier switch
        {
            null => null,
            CodexServiceTier.Normal => "default",
            CodexServiceTier.Fast => "fast",
            CodexServiceTier.Ultrafast => "ultrafast",
            _ => throw new ArgumentOutOfRangeException(nameof(CodexAgentOptions.ServiceTier))
        });
        Set(parameters, "sandbox", _options.Permissions switch
        {
            null => null,
            CodexPermissions.ReadOnly => "read-only",
            CodexPermissions.WorkspaceWrite => "workspace-write",
            CodexPermissions.FullAccess => "danger-full-access",
            _ => throw new ArgumentOutOfRangeException(nameof(CodexAgentOptions.Permissions))
        });
        Set(parameters, "approvalPolicy", _options.ApprovalPolicy switch
        {
            null => null,
            CodexApprovalPolicy.Never => "never",
            CodexApprovalPolicy.OnRequest => "on-request",
            _ => throw new ArgumentOutOfRangeException(nameof(CodexAgentOptions.ApprovalPolicy))
        });
        if (_options.Config is not null)
        {
            parameters["config"] = _options.Config.DeepClone();
        }
        if (!resume && _tools is not null)
        {
            parameters["dynamicTools"] = _tools.CreateDefinitions();
        }
        CodexClient.Merge(parameters, resume ? _options.ThreadResumeParameters : _options.ThreadStartParameters);
        JsonObject config = parameters["config"] as JsonObject ?? new JsonObject();
        config["features.default_mode_request_user_input"] = _options.OnUserInputRequested is not null;
        parameters["config"] = config;
        return parameters;
    }

    private async Task<CodexServerResponse> HandleServerRequestAsync(CodexServerRequest request, CancellationToken cancellationToken)
    {
        string method = request.Method;
        JsonElement parameters = request.RawRepresentation;
        if (method == "item/tool/call")
        {
            if (_tools?.CanHandle(parameters) != true)
            {
                return request.RespondWithNativeResult(CodexFunctionTools.Result("No C# function is registered for the requested tool.", false));
            }
            ToolInvocationScope? scope = _toolScope;
            if (scope is null || parameters.GetProperty("threadId").GetString() != scope.ThreadId ||
                (scope.TurnId is not null && parameters.GetProperty("turnId").GetString() != scope.TurnId))
            {
                return request.RespondWithNativeResult(CodexFunctionTools.Result("No matching active Codex turn for this tool invocation.", false));
            }
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.CancellationToken);
            return request.RespondWithNativeResult(await _tools.InvokeAsync(parameters, this, _options, scope.Messages, scope.IsStreaming, linked.Token).ConfigureAwait(false));
        }
        if (request.Kind is CodexServerRequestKind.CommandApproval or CodexServerRequestKind.FileChangeApproval)
        {
            ToolInvocationScope? scope = _toolScope;
            if (scope is null || (request.ThreadId is not null && request.ThreadId != scope.ThreadId) ||
                (request.TurnId is not null && scope.TurnId is not null && request.TurnId != scope.TurnId))
            {
                return request.Decline();
            }
            string callId = Guid.NewGuid().ToString("N");
            Dictionary<string, object?> arguments = parameters.EnumerateObject().ToDictionary(property => property.Name, property => (object?)property.Value.Clone());
            FunctionCallContent call = new(callId, request.Kind == CodexServerRequestKind.CommandApproval ? "codex_command" : "codex_file_change", arguments)
            {
                RawRepresentation = parameters
            };
            using CancellationTokenSource approvalLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.CancellationToken);
            bool approved = await RequestApprovalAsync(call, approvalLifetime.Token).ConfigureAwait(false);
            return approved ? request.Approve() : request.Decline();
        }
        if (request.Kind == CodexServerRequestKind.UserInput)
        {
            if (_options.OnUserInputRequested is not { } handler)
            {
                throw new NotSupportedException("User input is disabled. Complete the task without asking the user questions.");
            }
            ToolInvocationScope? scope = _toolScope;
            if (scope is null || (request.ThreadId is not null && request.ThreadId != scope.ThreadId) ||
                (request.TurnId is not null && scope.TurnId is not null && request.TurnId != scope.TurnId))
            {
                throw new NotSupportedException("No matching active Codex turn for this question.");
            }
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.CancellationToken);
            CodexUserInputRequest userInput = new(request);
            CodexUserInputResponse response = await handler(userInput, linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
            return userInput.GetServerResponse(response);
        }
        throw new NotSupportedException("Unsupported Codex request: " + method);
    }

    internal async Task<bool> RequestApprovalAsync(FunctionCallContent call, CancellationToken cancellationToken)
    {
        string requestId = Guid.NewGuid().ToString("N");
        // Approval messages are descriptive snapshots; callers cannot change the executable arguments.
        Dictionary<string, object?>? arguments = call.Arguments?.ToDictionary(entry => entry.Key,
            entry => entry.Value is null ? null : (object?)JsonSerializer.SerializeToElement(entry.Value));
        ToolApprovalRequestContent content = new(requestId, new FunctionCallContent(call.CallId, call.Name, arguments))
        {
            RawRepresentation = call.RawRepresentation
        };
        PendingApproval approval = new(content);
        _approvals[requestId] = approval;
        try
        {
            Client.PublishApproval(requestId);
            return await approval.Decision.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _approvals.TryRemove(requestId, out _);
        }
    }

    private sealed class PendingApproval(ToolApprovalRequestContent content)
    {
        public ToolApprovalRequestContent Content { get; } = content;
        public TaskCompletionSource<bool> Decision { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ToolInvocationScope(string threadId, IList<ChatMessage> messages, bool isStreaming, CancellationToken cancellationToken)
    {
        public string ThreadId { get; } = threadId;
        public string? TurnId { get; set; }
        public IList<ChatMessage> Messages { get; } = messages;
        public bool IsStreaming { get; } = isStreaming;
        public CancellationToken CancellationToken { get; } = cancellationToken;
    }

    private static void Set(JsonObject parameters, string key, string? value)
    {
        if (value is not null)
        {
            parameters[key] = value;
        }
    }

    private static string? GetMessagePhase(JsonElement item)
    {
        return item.TryGetProperty("phase", out JsonElement phase) && phase.ValueKind == JsonValueKind.String
            ? phase.GetString()
            : null;
    }

    private static CodexAgentSession GetSession(AgentSession session)
    {
        return session as CodexAgentSession ?? throw new ArgumentException("This agent requires a CodexAgentSession.", nameof(session));
    }

    private static JsonArray CreateInput(IEnumerable<ChatMessage> messages)
    {
        JsonArray input = [];
        foreach (ChatMessage message in messages)
        {
            if (message.Role != ChatRole.User)
            {
                throw new NotSupportedException("Codex accepts user input; configure instructions through CodexAgentOptions and preserve conversation history through its native session.");
            }
            foreach (AIContent content in message.Contents)
            {
                if (content is TextContent text)
                {
                    input.Add(new JsonObject { ["type"] = "text", ["text"] = text.Text });
                }
                else if (content is UriContent uri && uri.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    input.Add(new JsonObject { ["type"] = "image", ["url"] = uri.Uri.ToString() });
                }
                else if (content is DataContent data && data.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    input.Add(new JsonObject { ["type"] = "image", ["url"] = data.Uri });
                }
                else
                {
                    throw new NotSupportedException("Codex input supports text and images. Use native turn parameters for other Codex input types.");
                }
            }
        }
        return input;
    }

    /// <summary>Cancels pending runs and disposes the app-server client when this agent owns it. Abandoning an active native turn also closes its client.</summary>
    /// <returns>A task representing shutdown.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try
        {
            await _shutdown.CancelAsync().ConfigureAwait(false);
        }
        finally
        {
            await _runLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await ClearRunAsync().ConfigureAwait(false);
            }
            finally
            {
                _runLock.Release();
                _shutdown.Dispose();
                if (_ownsClient)
                {
                    await Client.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
