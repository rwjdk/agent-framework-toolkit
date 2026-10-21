using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>An Agent Framework agent controlling a local Codex app-server.</summary>
[PublicAPI]
public sealed class CodexAgent : AIAgent, IAsyncDisposable
{
    private readonly CodexAgentOptions _options;
    private readonly bool _ownsClient;
    private readonly CodexFunctionTools? _tools;
    private ToolInvocationScope? _toolScope;

    /// <summary>Creates an agent using an existing Codex client.</summary>
    /// <param name="client">A client dedicated to this agent's event stream.</param>
    /// <param name="options">Thread and agent settings, including the required working directory.</param>
    /// <param name="ownsClient">Whether disposing this agent also disposes the client.</param>
    public CodexAgent(CodexClient client, CodexAgentOptions options, bool ownsClient = false)
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
            Client.OnRunResponseRequired = HandleServerRequestAsync;
        }
        else
        {
            Client.OnRunResponseRequired = _options.OnRunResponseRequired;
        }
    }

    /// <summary>The underlying client for native account, model, thread, and configuration operations.</summary>
    public CodexClient Client { get; }

    /// <inheritdoc />
    protected override string? IdCore => _options.Id;

    /// <inheritdoc />
    public override string? Name => _options.Name;

    /// <inheritdoc />
    public override string? Description => _options.Description;

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null && serviceType == typeof(CodexClient))
        {
            return Client;
        }
        return base.GetService(serviceType, serviceKey);
    }

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
        return parameters;
    }

    private async Task<CodexRunResponse> HandleServerRequestAsync(CodexRunRequest request, CancellationToken cancellationToken)
    {
        string method = request.Method;
        JsonElement parameters = request.RawRepresentation;
        if (method == "item/tool/call" && _tools!.CanHandle(parameters))
        {
            ToolInvocationScope? scope = _toolScope;
            if (scope is null || parameters.GetProperty("threadId").GetString() != scope.ThreadId ||
                (scope.TurnId is not null && parameters.GetProperty("turnId").GetString() != scope.TurnId))
            {
                return request.CompleteTool("No matching active Codex turn for this tool invocation.", false);
            }
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.CancellationToken);
            return request.RespondWithNativeResult(await _tools.InvokeAsync(parameters, this, _options, scope.Messages, scope.IsStreaming, linked.Token).ConfigureAwait(false));
        }
        if (_options.OnRunResponseRequired is not null)
        {
            return await _options.OnRunResponseRequired(request, cancellationToken).ConfigureAwait(false);
        }
        return method == "item/tool/call"
            ? request.CompleteTool("No C# function is registered for the requested tool.", false)
            : request.RespondWithNativeResult(CodexClient.DefaultServerRequestResult(method));
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

    /// <summary>Disposes the app-server client when this agent owns it.</summary>
    /// <returns>A task representing shutdown.</returns>
    public ValueTask DisposeAsync()
    {
        return _ownsClient ? Client.DisposeAsync() : ValueTask.CompletedTask;
    }
}
