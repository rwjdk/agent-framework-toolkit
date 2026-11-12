using JetBrains.Annotations;

namespace AgentFrameworkToolkit.OpenAI.Batching;

/// <summary>
/// Options for a Chat batch run.
/// </summary>
[PublicAPI]
public class ChatBatchOptions
{
    /// <summary>
    /// Model/Deployment name used for the requests.
    /// </summary>
    public required string Model { get; set; }

    /// <summary>
    /// If the run should wait until completed
    /// </summary>
    public bool WaitUntilCompleted { get; set; }

    /// <summary>
    /// Type ofg batch.
    /// </summary>
    public ChatBatchClientType ClientType { get; set; } = ChatBatchClientType.ChatClient;

    /// <summary>
    /// Optional instructions that are prepended to every batch line as a system message.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Reasoning effort when using reasoning-capable models.
    /// </summary>
    public OpenAIReasoningEffort? ReasoningEffort { get; set; }

    /// <summary>
    /// Reasoning summary verbosity (Only used when ClientType is ResponsesAPI)
    /// </summary>
    public OpenAIReasoningSummaryVerbosity? ReasoningSummaryVerbosity { get; set; }
}
