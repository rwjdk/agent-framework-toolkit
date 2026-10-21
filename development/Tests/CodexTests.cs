using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace AgentFrameworkToolkit.Tests;

#pragma warning disable AFT999 // Exercise the experimental Codex provider.

public class CodexTests
{
    [Fact]
    public async Task ClientDisposesTransportsWhenCancellationCallbackThrowsAsync()
    {
        ScriptedServer server = new() { RequestApproval = true };
        await using CodexClient client = server.CreateClient();
        client.OnRunResponseRequired = (request, token) =>
        {
            token.Register(() => throw new InvalidOperationException("Cancellation callback failed"));
            return Task.FromResult(request.Approve());
        };
        await client.RequestAsync("turn/start", cancellationToken: TestContext.Current.CancellationToken);
        await server.ServerResponseWritten.Task.WaitAsync(TestContext.Current.CancellationToken);

        AggregateException error = await Assert.ThrowsAsync<AggregateException>(() => client.DisposeAsync().AsTask());

        Assert.Contains(error.Flatten().InnerExceptions, e => e.Message == "Cancellation callback failed");
        Assert.True(server.InputDisposed);
        Assert.True(server.OutputDisposed);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task AbandonedRunReleasesGateWhenClientDisposalThrowsAsync()
    {
        ScriptedServer server = new() { RequestApproval = true, HoldTurn = true, EmitTurnStarted = true };
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            OnRunResponseRequired = (request, token) =>
            {
                token.Register(() => throw new InvalidOperationException("Cancellation callback failed"));
                return Task.FromResult(request.Approve());
            }
        });
        await using IAsyncEnumerator<AgentResponseUpdate> stream = agent.RunStreamingAsync("Wait", cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await stream.MoveNextAsync());
        await server.ServerResponseWritten.Task.WaitAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<AggregateException>(() => stream.DisposeAsync().AsTask());

        Assert.Contains(server.Requests, r => r.GetProperty("method").GetString() == "turn/interrupt");
        Assert.True(server.InputDisposed);
        Assert.True(server.OutputDisposed);
        Assert.True(await client.RunGate.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
        client.RunGate.Release();
    }

    [Fact]
    public async Task AbandonedRunStillInterruptsAndDisposesWhenToolCancellationThrowsAsync()
    {
        ScriptedServer server = new() { RequestTool = true, ToolName = "wait", ToolArguments = new(), EmitTurnStarted = true };
        await using CodexClient client = server.CreateClient();
        TaskCompletionSource toolStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> releaseTool = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> wait = async token =>
        {
            using CancellationTokenRegistration registration = token.Register(() => throw new InvalidOperationException("Tool cancellation failed"));
            toolStarted.TrySetResult();
            return await releaseTool.Task;
        };
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(), Tools = [AIFunctionFactory.Create(wait, "wait")]
        });
        await using IAsyncEnumerator<AgentResponseUpdate> stream = agent.RunStreamingAsync("Wait", cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await stream.MoveNextAsync());
        await toolStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            await Assert.ThrowsAsync<AggregateException>(() => stream.DisposeAsync().AsTask());

            Assert.Contains(server.Requests, r => r.GetProperty("method").GetString() == "turn/interrupt");
            Assert.True(server.InputDisposed);
            Assert.True(server.OutputDisposed);
            Assert.True(await client.RunGate.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            client.RunGate.Release();
        }
        finally
        {
            releaseTool.TrySetResult("Done");
        }
    }

    [Theory]
    [InlineData("orders", "orders")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void ToolRequestAccessorsPreserveNamespaceAndArgumentsAfterDocumentDisposal(string? toolNamespace, string? expectedNamespace)
    {
        CodexRunRequest request;
        using (JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            tool = "lookup", @namespace = toolNamespace, arguments = new { orderId = 123, filter = new { includeArchived = true } }
        })))
        {
            request = new CodexRunRequest("item/tool/call", document.RootElement);
        }
        Assert.Equal("lookup", request.ToolName);
        Assert.Equal(expectedNamespace, request.ToolNamespace);
        Assert.Equal(123, request.ToolArguments!.Value.GetProperty("orderId").GetInt32());
        Assert.True(request.ToolArguments.Value.GetProperty("filter").GetProperty("includeArchived").GetBoolean());

        CodexRunRequest missing = new("item/tool/call", JsonSerializer.SerializeToElement(new { tool = "no_arguments" }));
        Assert.Null(missing.ToolNamespace);
        Assert.Null(missing.ToolArguments);
    }

    [Theory]
    [InlineData("item/started", "spawnAgent", "inProgress", "running", "Investigating dependencies")]
    [InlineData("item/completed", "wait", "completed", "completed", "Dependency review finished")]
    public void CollaborationEventsDescribeTaskRecipientsAndIndividualAgentOutcomes(string method, string tool, string toolStatus, string agentStatus, string agentMessage)
    {
        JsonElement notification = JsonSerializer.SerializeToElement(new
        {
            method, @params = new { threadId = "thread-1", turnId = "turn-1", item = new
            {
                type = "collabAgentToolCall", id = "collab-1", tool, status = toolStatus,
                prompt = "Review dependencies for breaking changes", senderThreadId = "thread-1",
                receiverThreadIds = new[] { "agent-1", "agent-2" },
                agentsStates = new Dictionary<string, object>
                {
                    ["agent-1"] = new { status = agentStatus, message = agentMessage },
                    ["agent-2"] = new { status = "errored", message = "Repository unavailable" }
                }
            } }
        });
        CodexRunEvent runEvent = CodexRunEvent.FromEvent(notification)!;
        Assert.Equal("Review dependencies for breaking changes", runEvent.AgentTask);
        Assert.Equal(new[] { "agent-1", "agent-2" }, runEvent.ReceivingThreadIds);
        Assert.Equal(2, runEvent.AgentStates.Count);
        Assert.Equal(agentStatus, runEvent.AgentStates[0].Status);
        Assert.Equal(agentMessage, runEvent.AgentStates[0].Message);
        Assert.Equal("errored", runEvent.AgentStates[1].Status);
        Assert.Contains("Task: Review dependencies", runEvent.Summary);
        Assert.Contains("Receiving agents: agent-1, agent-2", runEvent.Summary);
        Assert.Contains($"Agent agent-1: {agentStatus} — {agentMessage}", runEvent.Summary);
        Assert.Contains("Agent agent-2: errored — Repository unavailable", runEvent.Text);
        Assert.NotNull(runEvent.ToolResult);
    }

    [Fact]
    public void CollaborationWaitWithoutPromptStillDescribesAgentOutcome()
    {
        CodexRunEvent runEvent = CodexRunEvent.FromEvent(JsonSerializer.SerializeToElement(new
        {
            method = "item/completed", @params = new { item = new
            {
                type = "collabAgentToolCall", id = "wait-1", tool = "wait", status = "completed",
                prompt = (string?)null, receiverThreadIds = new[] { "agent-1" },
                agentsStates = new Dictionary<string, object> { ["agent-1"] = new { status = "completed", message = "No issues found" } }
            } }
        }))!;
        Assert.Null(runEvent.AgentTask);
        Assert.Contains("Agent agent-1: completed — No issues found", runEvent.Summary);
    }

    [Theory]
    [InlineData("mcpServer/startupStatus/updated")]
    [InlineData("account/updated")]
    [InlineData("thread/status/changed")]
    [InlineData("thread/tokenUsage/updated")]
    [InlineData("warning")]
    [InlineData("configWarning")]
    [InlineData("future/notification")]
    public void UnselectedNotificationsAreNotRunEvents(string method)
    {
        Assert.Null(CodexRunEvent.FromEvent(JsonSerializer.SerializeToElement(new { method, @params = new { } })));
    }

    [Theory]
    [InlineData("failed", "Run failed: No credits")]
    [InlineData("interrupted", "Run interrupted: No credits")]
    public void RunCompletionIncludesOutcomeAndError(string status, string summary)
    {
        JsonElement notification = JsonSerializer.SerializeToElement(new
        {
            method = "turn/completed", @params = new { threadId = "thread-1", turn = new { id = "turn-1", status, error = new { message = "No credits" } } }
        });
        CodexRunEvent progress = CodexRunEvent.FromEvent(notification)!;
        Assert.Equal(summary, progress.Summary);
        Assert.Equal("No credits", progress.Error);
        Assert.Equal("turn-1", progress.TurnId);
        Assert.Equal(CodexRunEventStage.Completed, progress.Stage);
    }

    [Fact]
    public void ToolProgressIncludesArgumentsOutputAndFailureDetails()
    {
        JsonElement notification = JsonSerializer.SerializeToElement(new
        {
            method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new
            {
                type = "mcpToolCall", id = "tool-1", server = "orders", tool = "lookup", status = "failed",
                arguments = new { orderId = 123 }, result = new { content = new[] { new { type = "text", text = "Order not found" } } },
                error = new { message = "Lookup failed" }
            } }
        });
        CodexRunEvent progress = CodexRunEvent.FromEvent(notification)!;
        Assert.Equal("Tool lookup failed. Lookup failed", progress.Summary);
        Assert.Equal("orders", progress.ToolServer);
        Assert.Equal(123, progress.ToolArguments!.Value.GetProperty("orderId").GetInt32());
        Assert.Equal("Order not found", progress.Text);
        Assert.NotNull(progress.ToolResult);
    }

    [Fact]
    public void ReasoningAndErrorEventsExposeTheirContent()
    {
        CodexRunEvent reasoning = CodexRunEvent.FromEvent(JsonSerializer.SerializeToElement(new
        {
            method = "item/completed", @params = new { item = new { type = "reasoning", id = "reason-1", summary = new[] { "Checking dependencies", "Choosing the smallest fix" } } }
        }))!;
        Assert.Contains("Checking dependencies", reasoning.Text);
        Assert.Contains("Choosing the smallest fix", reasoning.Summary);
        CodexRunEvent error = CodexRunEvent.FromEvent(JsonSerializer.SerializeToElement(new
        {
            method = "error", @params = new { error = new { message = "Connection lost" }, willRetry = true }
        }))!;
        Assert.Equal("Connection lost Retrying.", error.Summary);
        Assert.True(error.WillRetry);
        Assert.Null(CodexRunEvent.FromEvent(JsonSerializer.SerializeToElement(new
        {
            method = "item/started", @params = new { item = new { type = "reasoning", id = "empty", summary = Array.Empty<string>() } }
        })));
    }

    [Theory]
    [InlineData("item/commandExecution/requestApproval", CodexRunRequestKind.CommandApproval)]
    [InlineData("item/fileChange/requestApproval", CodexRunRequestKind.FileChangeApproval)]
    public void ApprovalHelpersProduceNativeDecisions(string method, CodexRunRequestKind kind)
    {
        CodexRunRequest request = new(method, JsonSerializer.SerializeToElement(new
        {
            threadId = "thread-1", turnId = "turn-1", itemId = "item-1", command = "echo hello",
            cwd = "/work", reason = "Needs more permissions", grantRoot = "/outside"
        }));
        Assert.Equal(kind, request.Kind);
        Assert.Equal("echo hello", request.Command);
        Assert.Equal("Needs more permissions", request.Reason);
        Assert.Equal("/outside", request.RequestedWriteRoot);
        Assert.Equal("accept", request.GetResult(request.Approve())!["decision"]!.GetValue<string>());
        Assert.Equal("decline", request.GetResult(request.Decline())!["decision"]!.GetValue<string>());
        Assert.Equal("cancel", request.GetResult(request.Cancel())!["decision"]!.GetValue<string>());
        Assert.Throws<InvalidOperationException>(() => request.CompleteTool("wrong response"));
    }

    [Fact]
    public void TypedQuestionsAndNativeFallbackPreserveResponseShapes()
    {
        CodexRunRequest request = new("item/tool/requestUserInput", JsonSerializer.SerializeToElement(new
        {
            questions = new[] { new { id = "color", question = "Which color?", isSecret = false, options = new[] { new { label = "Blue", description = "Use blue" } } } }
        }));
        Assert.Equal("Which color?", Assert.Single(request.Questions).Question);
        Assert.Equal("Blue", Assert.Single(request.Questions[0].Options));
        CodexRunResponse response = request.AnswerQuestions(new Dictionary<string, string[]> { ["color"] = ["Blue"] });
        Assert.Equal("Blue", request.GetResult(response)!["answers"]!["color"]!["answers"]![0]!.GetValue<string>());
        Assert.Throws<InvalidOperationException>(() => request.Approve());
        Assert.Throws<ArgumentException>(() => request.AnswerQuestions(new Dictionary<string, string[]>()));

        CodexRunRequest other = new("future/request", JsonSerializer.SerializeToElement(new { detail = "test" }));
        Assert.Equal(CodexRunRequestKind.Other, other.Kind);
        CodexRunResponse native = other.RespondWithNativeResult(new JsonObject { ["custom"] = true });
        Assert.True(other.GetResult(native)!["custom"]!.GetValue<bool>());
        Assert.Throws<ArgumentException>(() => other.GetResult(response));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunEventsExcludeHousekeepingWarningsAndUnknownItemsAsync(bool streaming)
    {
        ScriptedServer server = new() { ReportOtherEvents = true };
        await using CodexClient client = server.CreateClient();
        List<CodexRunEvent> progress = [];
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(), OnRunEvent = progress.Add
        });
        if (streaming)
        {
            List<JsonElement> native = [];
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Hello", cancellationToken: TestContext.Current.CancellationToken))
            {
                if (update.RawRepresentation is JsonElement notification)
                {
                    native.Add(notification);
                }
            }
            Assert.Contains(native, n => n.GetProperty("method").GetString() == "warning");
            Assert.Contains(native, n => n.GetProperty("method").GetString() == "future/notification");
        }
        else
        {
            await agent.RunAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        }
        Assert.NotEmpty(progress);
        Assert.All(progress, p => Assert.False(string.IsNullOrWhiteSpace(p.Summary)));
        Assert.DoesNotContain(progress, p => p.Method is "future/notification" or "global/notification" or "null/notification" or "warning" or "configWarning");
        Assert.DoesNotContain(progress, p => p.ItemId == "future-item");
        Assert.Contains(progress, p => p.Kind == CodexRunEventKind.Run && p.Stage == CodexRunEventStage.Completed && p.TurnId == "turn-1");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ToolMiddlewareCanModifyOrShortCircuitInvocationsAsync(bool streaming, bool shortCircuit)
    {
        ScriptedServer server = new() { RequestTool = true };
        await using CodexClient client = server.CreateClient();
        List<string> calls = [];
        AIAgent? callingAgent = null;
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            Tools = [AIFunctionFactory.Create((int a, int b) => { calls.Add("function"); return a + b; }, "add")],
            ToolCallingMiddleware = async (caller, context, next, token) =>
            {
                callingAgent = caller;
                Assert.Equal(streaming, context.IsStreaming);
                Assert.Equal("call-1", context.CallContent.CallId);
                Assert.Equal("Add numbers", Assert.Single(context.Messages).Text);
                calls.Add("before");
                if (shortCircuit)
                {
                    return "blocked";
                }
                context.Arguments["a"] = 10;
                object? result = await next(context, token);
                calls.Add("after");
                return "result:" + result;
            },
            RawToolCallDetails = details => { Assert.Equal("add", details.Context.Function.Name); calls.Add("raw"); }
        });
        if (streaming)
        {
            await agent.RunStreamingAsync("Add numbers", cancellationToken: TestContext.Current.CancellationToken)
                .ToAgentResponseAsync(TestContext.Current.CancellationToken);
        }
        else
        {
            await agent.RunAsync("Add numbers", cancellationToken: TestContext.Current.CancellationToken);
        }
        Assert.Same(agent, callingAgent);
        Assert.Equal(shortCircuit ? ["before", "raw"] : new[] { "before", "function", "after", "raw" }, calls);
        JsonElement resultContent = server.ServerResponse!.Value.GetProperty("result").GetProperty("contentItems")[0];
        Assert.Equal(shortCircuit ? "blocked" : "result:13", resultContent.GetProperty("text").GetString());
    }

    [Fact]
    public async Task TypedProgressIncludesCommandsAndPreservesRawNotificationsAsync()
    {
        ScriptedServer server = new() { ReportCommands = true, ReportFileChanges = true, Commentary = "Working" };
        await using CodexClient client = server.CreateClient();
        List<CodexRunEvent> progress = [];
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(), OnRunEvent = progress.Add
        });
        AgentResponse response = await agent.RunAsync("Change files", cancellationToken: TestContext.Current.CancellationToken);
        CodexRunEvent started = Assert.Single(progress, p => p.Kind == CodexRunEventKind.Command && p.Stage == CodexRunEventStage.Started);
        Assert.Equal("echo hello", started.Command);
        Assert.Null(started.ExitCode);
        CodexRunEvent delta = Assert.Single(progress, p => p.Kind == CodexRunEventKind.Command && p.Stage == CodexRunEventStage.Delta);
        Assert.Equal("hello", delta.Text);
        CodexRunEvent completed = Assert.Single(progress, p => p.Kind == CodexRunEventKind.Command && p.Stage == CodexRunEventStage.Completed);
        Assert.Equal(0, completed.ExitCode);
        Assert.Equal(TimeSpan.FromMilliseconds(12), completed.Duration);
        Assert.Equal(started.ItemId, delta.ItemId);
        Assert.Contains(progress, p => p.Kind == CodexRunEventKind.Message && p.MessagePhase == "commentary");
        Assert.Contains(progress, p => p.Kind == CodexRunEventKind.FileChange && p.Status == "failed" && p.FileChanges.Count == 3);
        Assert.DoesNotContain(progress, p => p.ThreadId == "other-thread");
        Assert.Equal("item/completed", completed.RawRepresentation.GetProperty("method").GetString());
        Assert.Contains(progress, p => p.Kind == CodexRunEventKind.Run && p.Method == "turn/completed" && p.Status == "completed");
        Assert.All(progress, p => Assert.False(string.IsNullOrWhiteSpace(p.Summary)));
        Assert.Equal("Hello", response.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedFileChangesSurviveResponseAggregationAsync(bool streaming)
    {
        ScriptedServer server = new() { ReportFileChanges = true };
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });
        AgentSession session = await agent.CreateSessionAsync(TestContext.Current.CancellationToken);

        AgentResponse response = streaming
            ? await agent.RunStreamingAsync("Change files", session, cancellationToken: TestContext.Current.CancellationToken)
                .ToAgentResponseAsync(TestContext.Current.CancellationToken)
            : await agent.RunAsync("Change files", session, cancellationToken: TestContext.Current.CancellationToken);

        IReadOnlyList<CodexFileChange> changes = response.GetFileChanges();
        Assert.Equal(3, changes.Count);
        Assert.Equal("x.txt", changes[0].Path);
        Assert.Equal(CodexFileChangeKind.Created, changes[0].Kind);
        Assert.Equal("+hello", changes[0].Diff);
        Assert.Equal(CodexFileChangeKind.Modified, changes[1].Kind);
        Assert.Equal("moved.txt", changes[1].MovePath);
        Assert.Equal(CodexFileChangeKind.Deleted, changes[2].Kind);
        Assert.Equal("Hello", response.Text);

        server.ReportFileChanges = false;
        AgentResponse next = await agent.RunAsync("Again", session, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(next.GetFileChanges());
    }

    [Theory]
    [InlineData("add", true)]
    [InlineData("unknown_tool", false)]
    public async Task RegisteredFunctionsAreInvokedAndRemainAvailableAfterResumeAsync(string requestedTool, bool success)
    {
        ScriptedServer server = new() { RequestTool = true, ToolName = requestedTool };
        await using CodexClient client = server.CreateClient();
        int invocations = 0;
        AIFunction add = AIFunctionFactory.Create((int a, int b) =>
        {
            invocations++;
            return a + b;
        }, "add", "Adds two numbers.");
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            Tools = [add]
        });
        AgentSession session = await agent.CreateSessionAsync(TestContext.Current.CancellationToken);

        await agent.RunAsync("Add numbers", session, cancellationToken: TestContext.Current.CancellationToken);
        await agent.RunAsync("Again", session, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(success ? 2 : 0, invocations);
        JsonElement result = server.ServerResponse!.Value.GetProperty("result");
        Assert.Equal(success, result.GetProperty("success").GetBoolean());
        if (success)
        {
            Assert.Equal("5", result.GetProperty("contentItems")[0].GetProperty("text").GetString());
        }
        JsonElement initialize = Assert.Single(server.Requests, request => request.GetProperty("method").GetString() == "initialize");
        Assert.True(initialize.GetProperty("params").GetProperty("capabilities").GetProperty("experimentalApi").GetBoolean());
        JsonElement start = Assert.Single(server.Requests, request => request.GetProperty("method").GetString() == "thread/start");
        JsonElement definition = start.GetProperty("params").GetProperty("dynamicTools")[0];
        Assert.Equal("add", definition.GetProperty("name").GetString());
        Assert.True(definition.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("a", out _));
        JsonElement resume = Assert.Single(server.Requests, request => request.GetProperty("method").GetString() == "thread/resume");
        Assert.False(resume.GetProperty("params").TryGetProperty("dynamicTools", out _));
    }

    [Fact]
    public async Task FunctionExceptionsReturnToolFailureWithoutFailingTheTurnAsync()
    {
        ScriptedServer server = new() { RequestTool = true };
        await using CodexClient client = server.CreateClient();
        Func<int, int, int> fail = (a, b) => throw new InvalidOperationException("Expected tool failure");
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            Tools = [AIFunctionFactory.Create(fail, "add")]
        });

        AgentResponse response = await agent.RunAsync("Call the tool", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Hello", response.Text);
        Assert.False(server.ServerResponse!.Value.GetProperty("result").GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CancellingRunCancelsRegisteredFunctionAsync()
    {
        ScriptedServer server = new() { RequestTool = true, ToolName = "wait", ToolArguments = new JsonObject() };
        await using CodexClient client = server.CreateClient();
        TaskCompletionSource invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> wait = async token =>
        {
            invoked.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, token);
                return "Unexpected completion";
            }
            finally
            {
                cancelled.TrySetResult();
            }
        };
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            Tools = [AIFunctionFactory.Create(wait, "wait")]
        });
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<AgentResponse> run = agent.RunAsync("Wait", cancellationToken: cancellation.Token);
        await invoked.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        await cancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true, "final_answer")]
    [InlineData(false, "final_answer")]
    [InlineData(false, null)]
    public async Task StructuredResponseExcludesProgressMessagesAsync(bool emitStarted, string? finalPhase)
    {
        const string json = "{\"title\":\"The Shawshank Redemption\",\"yearOfRelease\":1994}";
        ScriptedServer server = new()
        {
            ResponseText = json,
            Commentary = "I'll check IMDb's top-rated movie.",
            EmitStarted = emitStarted,
            MessagePhase = finalPhase
        };
        await using CodexClient client = server.CreateClient();
        bool commentaryObserved = false;
        await using CodexAgent agent = new(client, new CodexAgentOptions
        {
            WorkingDirectory = Path.GetTempPath(),
            OnRunEvent = notification =>
            {
                if (notification.Stage == CodexRunEventStage.Completed && notification.ItemId == "progress-1")
                {
                    commentaryObserved = true;
                }
            }
        });

        AgentResponse<Movie> response = await agent.RunAsync<Movie>("Top IMDb movie", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("The Shawshank Redemption", response.Result.Title);
        Assert.Equal(1994, response.Result.YearOfRelease);
        Assert.Equal(json, response.Text);
        Assert.True(commentaryObserved);
    }

    [Fact]
    public async Task GeneratedStructuredOutputSchemaIsNormalizedForCodexAsync()
    {
        ScriptedServer server = new() { ResponseText = "{\"title\":\"Arrival\",\"yearOfRelease\":2016}" };
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });

        AgentResponse<Movie> response = await agent.RunAsync<Movie>("Name a movie", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Arrival", response.Result.Title);
        JsonElement request = Assert.Single(server.Requests, request => request.GetProperty("method").GetString() == "turn/start");
        JsonElement schema = request.GetProperty("params").GetProperty("outputSchema");
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(2, schema.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public async Task NestedObjectSchemasAreNormalizedWithoutChangingCallerSchemaAsync()
    {
        ChatResponseFormatJson format = ChatResponseFormat.ForJsonSchema<MovieCollection>();
        string originalSchema = format.Schema!.Value.GetRawText();
        ScriptedServer server = new();
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });

        await agent.RunAsync("List movies", options: new AgentRunOptions { ResponseFormat = format }, cancellationToken: TestContext.Current.CancellationToken);

        JsonElement request = Assert.Single(server.Requests, request => request.GetProperty("method").GetString() == "turn/start");
        JsonElement schema = request.GetProperty("params").GetProperty("outputSchema");
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.False(schema.GetProperty("properties").GetProperty("movies").GetProperty("items").GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(originalSchema, format.Schema.Value.GetRawText());
    }

    private sealed record Movie(string Title, int YearOfRelease);
    private sealed record MovieCollection(Movie[] Movies);

    [Fact]
    public async Task StreamingDoesNotDuplicateCompletedMessagesAndResumesNativeThreadAsync()
    {
        ScriptedServer server = new();
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });
        AgentSession session = await agent.CreateSessionAsync(TestContext.Current.CancellationToken);

        AgentResponse response = await agent.RunAsync("first", session, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Hello", response.Text);
        Assert.Equal("thread-1", session.GetThreadId());

        JsonElement state = await agent.SerializeSessionAsync(session, cancellationToken: TestContext.Current.CancellationToken);
        AgentSession restored = await agent.DeserializeSessionAsync(state, cancellationToken: TestContext.Current.CancellationToken);
        AgentResponse followUp = await agent.RunAsync("second", restored, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Hello", followUp.Text);
        Assert.Contains(server.Requests, request => request.GetProperty("method").GetString() == "thread/resume" &&
            request.GetProperty("params").GetProperty("threadId").GetString() == "thread-1");
    }

    [Fact]
    public async Task NativeOverridesAndServerCallbacksArePreservedAsync()
    {
        ScriptedServer server = new() { RequestApproval = true };
        await using CodexClient client = server.CreateClient();
        bool approvalHandled = false;
        CodexAgentOptions options = new()
        {
            WorkingDirectory = Path.GetTempPath(),
            Permissions = CodexPermissions.FullAccess,
            ApprovalPolicy = CodexApprovalPolicy.OnRequest,
            ServiceTier = CodexServiceTier.Fast,
            ReasoningSummary = CodexReasoningSummary.Detailed,
            Tools = [AIFunctionFactory.Create(() => "unused", "unused")],
            ThreadStartParameters = new JsonObject { ["sandbox"] = "read-only" },
            OnRunResponseRequired = (request, cancellationToken) =>
            {
                approvalHandled = request.Kind == CodexRunRequestKind.FileChangeApproval;
                return Task.FromResult(request.Decline());
            }
        };
        await using CodexAgent agent = new(client, options);
        await agent.RunAsync("inspect", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(approvalHandled);
        Assert.Equal("decline", server.ServerResponse!.Value.GetProperty("result").GetProperty("decision").GetString());
        Assert.Contains(server.Requests, request => request.GetProperty("method").GetString() == "thread/start" &&
            request.GetProperty("params").GetProperty("sandbox").GetString() == "read-only" &&
            request.GetProperty("params").GetProperty("approvalPolicy").GetString() == "on-request" &&
            request.GetProperty("params").GetProperty("serviceTier").GetString() == "fast");
        Assert.Contains(server.Requests, request => request.GetProperty("method").GetString() == "turn/start" &&
            request.GetProperty("params").GetProperty("summary").GetString() == "detailed");
    }

    [Fact]
    public async Task FailedTurnsAreNotReturnedAsSuccessfulResponsesAsync()
    {
        ScriptedServer server = new() { FailTurn = true };
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });

        await Assert.ThrowsAsync<InvalidOperationException>(() => agent.RunAsync("fail", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellingActiveTurnInterruptsAndClosesConnectionAsync()
    {
        ScriptedServer server = new() { HoldTurn = true };
        await using CodexClient client = server.CreateClient();
        await using CodexAgent agent = new(client, new CodexAgentOptions { WorkingDirectory = Path.GetTempPath() });
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<AgentResponse> run = agent.RunAsync("wait", cancellationToken: cancellation.Token);
        await server.TurnStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Contains(server.Requests, request => request.GetProperty("method").GetString() == "turn/interrupt");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClosingOutputFaultsPendingRequestAsync()
    {
        ScriptedServer server = new();
        await using CodexClient client = server.CreateClient();
        await client.StartAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<IOException>(() => client.RequestAsync("close-output", cancellationToken: TestContext.Current.CancellationToken));
    }

    private sealed class ScriptedServer
    {
        private readonly Channel<string> _output = Channel.CreateUnbounded<string>();
        public List<JsonElement> Requests { get; } = [];
        public TaskCompletionSource TurnStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ServerResponseWritten { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool InputDisposed { get; private set; }
        public bool OutputDisposed { get; private set; }
        public bool EmitTurnStarted { get; init; }
        public bool RequestApproval { get; init; }
        public bool RequestTool { get; init; }
        public string ToolName { get; init; } = "add";
        public JsonObject ToolArguments { get; init; } = new() { ["a"] = 2, ["b"] = 3 };
        public bool FailTurn { get; init; }
        public bool HoldTurn { get; init; }
        public string ResponseText { get; init; } = "Hello";
        public bool ReportFileChanges { get; set; }
        public bool ReportCommands { get; init; }
        public bool ReportOtherEvents { get; init; }
        public string? Commentary { get; init; }
        public bool EmitStarted { get; init; } = true;
        public string? MessagePhase { get; init; } = "final_answer";
        public JsonElement? ServerResponse { get; private set; }

        public CodexClient CreateClient()
        {
            return new CodexClient(new OutputReader(_output.Reader, () => OutputDisposed = true), new InputWriter(Handle, () => InputDisposed = true));
        }

        private void Handle(string line)
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement request = document.RootElement.Clone();
            if (!request.TryGetProperty("method", out JsonElement method))
            {
                ServerResponse = request;
                ServerResponseWritten.TrySetResult();
                if (!HoldTurn)
                {
                    CompleteTurn();
                }
                return;
            }
            Requests.Add(request);
            if (!request.TryGetProperty("id", out JsonElement id))
            {
                return;
            }
            object result = new { };
            switch (method.GetString())
            {
                case "thread/start":
                case "thread/resume":
                    result = new { thread = new { id = "thread-1" } };
                    break;
                case "turn/start":
                    result = new { turn = new { id = "turn-1" } };
                    break;
                case "close-output":
                    _output.Writer.TryComplete();
                    return;
            }
            Send(new { id = id.GetInt32(), result });
            if (method.GetString() == "turn/start")
            {
                if (EmitTurnStarted)
                {
                    Send(new { method = "turn/started", @params = new { threadId = "thread-1", turn = new { id = "turn-1", status = "inProgress" } } });
                }
                if (RequestApproval)
                {
                    Send(new { id = "approval-1", method = "item/fileChange/requestApproval", @params = new { threadId = "thread-1" } });
                }
                else if (RequestTool)
                {
                    Send(new { id = "tool-1", method = "item/tool/call", @params = new
                    {
                        threadId = "thread-1", turnId = "turn-1", callId = "call-1", tool = ToolName, arguments = ToolArguments
                    } });
                }
                else if (!HoldTurn)
                {
                    CompleteTurn();
                }
                TurnStarted.TrySetResult();
            }
        }

        private void CompleteTurn()
        {
            if (ReportOtherEvents)
            {
                Send(new { method = "warning", @params = new { threadId = (string?)null, message = "Runtime warning details" } });
                Send(new { method = "configWarning", @params = new { summary = "Configuration issue", details = "Fix this setting" } });
                Send(new { method = "future/notification", @params = new { threadId = "thread-1", value = 42 } });
                Send(new { method = "global/notification" });
                Send(new { method = "null/notification", @params = (object?)null });
                Send(new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "futureItem", id = "future-item" } } });
            }
            if (ReportCommands)
            {
                Send(new { method = "item/started", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "commandExecution", id = "cmd-1", command = "echo hello", cwd = "/work", status = "inProgress", exitCode = (int?)null, durationMs = (long?)null } } });
                Send(new { method = "item/commandExecution/outputDelta", @params = new { threadId = "thread-1", turnId = "turn-1", itemId = "cmd-1", delta = "hello" } });
                Send(new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "commandExecution", id = "cmd-1", command = "echo hello", cwd = "/work", status = "completed", aggregatedOutput = "hello", exitCode = 0, durationMs = 12 } } });
            }
            if (ReportFileChanges)
            {
                object[] changes =
                [
                    new { path = "x.txt", kind = new { type = "add" }, diff = "+hello" },
                    new { path = "old.txt", kind = new { type = "update", move_path = "moved.txt" }, diff = "-old\n+new" },
                    new { path = "deleted.txt", kind = new { type = "delete" }, diff = "-bye" }
                ];
                object notification = new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "fileChange", id = "files-1", status = "completed", changes } } };
                Send(notification);
                Send(notification);
                Send(new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "fileChange", id = "failed-1", status = "failed", changes } } });
                Send(new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "fileChange", id = "declined-1", status = "declined", changes } } });
                Send(new { method = "item/completed", @params = new { threadId = "other-thread", turnId = "turn-1", item = new { type = "fileChange", id = "other-1", status = "completed", changes } } });
            }
            if (Commentary is not null)
            {
                SendMessage("progress-1", Commentary, MessagePhase is null ? null : "commentary");
            }
            SendMessage("message-1", ResponseText, MessagePhase);
            Send(new { method = "turn/completed", @params = new { threadId = "thread-1", turn = new { id = "turn-1", status = FailTurn ? "failed" : "completed", error = FailTurn ? new { message = "expected failure" } : null } } });
        }

        private void SendMessage(string id, string text, string? phase)
        {
            if (EmitStarted)
            {
                Send(new { method = "item/started", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "agentMessage", id, text = "", phase } } });
            }
            Send(new { method = "item/agentMessage/delta", @params = new { threadId = "thread-1", turnId = "turn-1", itemId = id, delta = text } });
            Send(new { method = "item/completed", @params = new { threadId = "thread-1", turnId = "turn-1", item = new { type = "agentMessage", id, text, phase } } });
        }

        private void Send(object value)
        {
            _output.Writer.TryWrite(JsonSerializer.Serialize(value));
        }
    }

    private sealed class OutputReader(ChannelReader<string> reader, Action disposed) : TextReader
    {
        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await reader.ReadAsync(cancellationToken);
            }
            catch (ChannelClosedException)
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            disposed();
            base.Dispose(disposing);
        }
    }

    private sealed class InputWriter(Action<string> write, Action disposed) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            write(buffer.ToString());
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            disposed();
            base.Dispose(disposing);
        }
    }
}
