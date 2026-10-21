using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;

#pragma warning disable AFT999

namespace Sandbox.Providers;

internal static class Codex
{
    public static async Task RunAsync()
    {
        CodexAgentFactory factory = new();
        await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
        {
            ApprovalPolicy = CodexApprovalPolicy.OnRequest,
            WorkingDirectory = "C:\\Users\\rasmu\\OneDrive\\Codex\\YouTube",
            OnRunEvent = HandleRunEvent,
            OnRunResponseRequired = HandleRunResponseRequiredAsync
        });
        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                AgentResponse response = await agent.RunAsync(input, session);
                Console.WriteLine(response);

                IReadOnlyList<CodexFileChange> fileChanges = response.GetFileChanges();
                foreach (CodexFileChange fileChange in fileChanges)
                {
                    Console.WriteLine($"- File: {fileChange.Path}");
                }

                Console.WriteLine("---");
            }
        }
    }

    private static void HandleRunEvent(CodexRunEvent runEvent)
    {
        // Completed snapshots contain the full text; displaying deltas too would repeat it.
        if (runEvent.Stage == CodexRunEventStage.Delta ||
            (runEvent.Kind == CodexRunEventKind.Message && runEvent.MessagePhase != "commentary"))
        {
            return;
        }

        Console.WriteLine($"[{runEvent.Kind}] {runEvent.Summary}");
        if (!string.IsNullOrWhiteSpace(runEvent.Details))
        {
            Console.WriteLine(runEvent.Details);
        }
        if (runEvent.Kind == CodexRunEventKind.Command && runEvent.Stage == CodexRunEventStage.Completed &&
            !string.IsNullOrWhiteSpace(runEvent.Text))
        {
            Console.WriteLine(runEvent.Text.TrimEnd());
        }
        if (runEvent.Kind == CodexRunEventKind.ToolCall)
        {
            if (runEvent.Stage == CodexRunEventStage.Started && runEvent.ToolArguments is { } arguments)
            {
                Console.WriteLine($"  Arguments: {arguments}");
            }
            if (runEvent.Stage == CodexRunEventStage.Completed)
            {
                if (!string.IsNullOrWhiteSpace(runEvent.Text) && !runEvent.Summary.Contains(runEvent.Text, StringComparison.Ordinal))
                {
                    Console.WriteLine(runEvent.Text);
                }
                else if (string.IsNullOrWhiteSpace(runEvent.Text) && runEvent.ToolResult is { } result)
                {
                    Console.WriteLine($"  Result: {result}");
                }
            }
        }
    }

    private static Task<CodexRunResponse> HandleRunResponseRequiredAsync(CodexRunRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // The default Never policy usually avoids approvals. If Codex requests one, accept automatically.
        // Use ApprovalPolicy.OnRequest to allow Codex to request approvals when needed.
        if (request.Kind is CodexRunRequestKind.CommandApproval or CodexRunRequestKind.FileChangeApproval)
        {
            Console.WriteLine($"[Automatically approved] {request.Kind}: {request.Command ?? request.RequestedWriteRoot ?? request.ItemId}");
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                Console.WriteLine($"  Reason: {request.Reason}");
            }
            return Task.FromResult(request.Approve());
        }
        // Registered C# tools run automatically. Add an application-specific answer strategy for user-input requests.
        throw new NotSupportedException($"No response handler for {request.Kind} ({request.Method}).");
    }
}
