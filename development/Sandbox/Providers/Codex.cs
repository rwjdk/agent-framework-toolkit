using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable AFT999

namespace Sandbox.Providers;

internal static class Codex
{
    public static async Task RunAsync()
    {
        CodexAgentFactory factory = new();
        await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
        {
            WorkingDirectory = "C:\\Users\\rasmu\\OneDrive\\Codex\\YouTube",
            OnRunEvent = HandleRunEvent,
            OnUserInputRequested = HandleUserInputRequestedAsync
        });
        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                AgentResponse response = await agent.RunAsync(input, session);
                while (response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().ToList() is { Count: > 0 } approvals)
                {
                    List<AIContent> decisions = [];
                    foreach (ToolApprovalRequestContent approval in approvals)
                    {
                        FunctionCallContent call = (FunctionCallContent)approval.ToolCall;
                        DisplayApproval(call);
                        Console.Write("Approve? [y/N] ");
                        decisions.Add(approval.CreateResponse(string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase)));
                    }
                    response = await agent.RunAsync(new ChatMessage(ChatRole.User, decisions), session);
                }
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

    private static void DisplayApproval(FunctionCallContent call)
    {
        Console.WriteLine();
        if (call.Name is "codex_command" or "codex_file_change")
        {
            Console.WriteLine(call.Name == "codex_command" ? "Permission to run a command" : "Permission to edit files");
            DisplayArgument("reason", "Reason");
            DisplayArgument("cwd", "Working directory");
            DisplayArgument("grantRoot", "Requested write directory");
            DisplayArgument("command", "Command");
        }
        else
        {
            Console.WriteLine($"Permission to call {call.Name}");
            if (call.Arguments is not null)
            {
                foreach (string name in call.Arguments.Keys)
                {
                    DisplayArgument(name, name);
                }
            }
        }

        void DisplayArgument(string name, string label)
        {
            if (call.Arguments?.TryGetValue(name, out object? value) == true &&
                value?.ToString() is { } text && !string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"{label}: {text}");
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

    private static Task<CodexUserInputResponse> HandleUserInputRequestedAsync(CodexUserInputRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<string, string[]> answers = [];
        foreach (CodexUserInputQuestion question in request.Questions)
        {
            Console.WriteLine(question.Question);
            if (question.Options.Count > 0)
            {
                Console.WriteLine(string.Join(" / ", question.Options));
            }
            answers[question.Id] = [Console.ReadLine() ?? ""];
        }
        return Task.FromResult(request.AnswerQuestions(answers));
    }
}
