using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using static Microsoft.Agents.AI.ChatClientAgentOptions;

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// Options for an OpenAI-Based Agent
/// </summary>
public class AgentOptions
{
    /// <summary>
    /// The type of OpenAI Client to use (ChatClient or ResponsesAPI)
    /// </summary>
    public ClientType? ClientType { get; set; }

    /// <summary>
    /// Model to use
    /// </summary>
    public required string Model { get; set; }

    /// <summary>
    /// Id of the Agent
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The Name of the Agent (Optional in most cases, but some scenarios to require one)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The Description of the Agent (Information only and not used by the LLM)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Instruction for the Agent to be fed to the LLM as System/Developer Message
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// A set of Tools that the Agent are allowed to call
    /// </summary>
    public IList<AITool>? Tools { get; set; }

    /// <summary>
    /// An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM
    /// </summary>
    public Action<RawCallDetails>? RawHttpCallDetails { get; set; }

    /// <summary>
    /// An Action, if set, will apply Tool Calling Middleware so you can inspect Tool Call Details
    /// </summary>
    public Action<ToolCallingDetails>? RawToolCallDetails { get; set; }

    /// <summary>
    /// The maximum number of tokens in the generated chat response.
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// An Action that allow you to inject additional ChatClientAgentOptions settings beyond what these options can do
    /// </summary>
    public Action<ChatClientAgentOptions>? AdditionalChatClientAgentOptions { get; set; }

    /// <summary>
    /// An optional <see cref="IServiceProvider"/> to use for resolving services required by the <see cref="AIFunction"/> instances being invoked.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Optional logger factory for enabling logging within the agent.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Provides a way to customize the creation of the underlying <see cref="IChatClient"/> used by the agent.
    /// </summary>
    public Func<IChatClient, IChatClient>? ClientFactory { get; set; }

    /// <summary>The temperature for generating chat responses. [ONLY USED BY NON-REASONING MODELS]</summary>
    /// <remarks>
    /// This value controls the randomness of predictions made by the model. Use a lower value to decrease randomness in the response.
    /// </remarks>
    public float? Temperature { get; set; }

    /// <summary>
    /// Define the reasoning Effort [ONLY USED BY REASONING MODELS]
    /// </summary>
    public OpenAIReasoningEffort? ReasoningEffort { get; set; }

    /// <summary>
    /// Define the reasoning summary verbosity [ONLY USED BY REASONING MODELS USING THE RESPONSES API]
    /// </summary>
    public OpenAIReasoningSummaryVerbosity? ReasoningSummaryVerbosity { get; set; }

    /// <summary>
    /// Enable Tool Calling Middleware allowing you to inspect, manipulate and cancel a tool-call
    /// </summary>
    public MiddlewareDelegates.ToolCallingMiddlewareDelegate? ToolCallingMiddleware { get; set; }

    /// <summary>
    /// Enable OpenTelemetry Middleware for OpenTelemetry Logging
    /// </summary>
    public OpenTelemetryMiddleware? OpenTelemetryMiddleware { get; set; }

    /// <summary>
    /// Enable Logging Middleware for custom Logging
    /// </summary>
    public LoggingMiddleware? LoggingMiddleware { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ChatHistoryProvider"/> instance to use for providing chat history for this agent.
    /// </summary>
    public ChatHistoryProvider? ChatHistoryProvider { get; set; }

    /// <summary>
    /// Gets or sets the list of <see cref="AIContextProvider"/> instances to use for providing additional context for each agent run.
    /// </summary>
    public IEnumerable<AIContextProvider>? AIContextProviders { get; set; }

    /// <summary>
    /// What service Tier to use (Only works for OpenAI directly)
    /// </summary>
    public OpenAIServiceTier? ServiceTier { get; set; }

    /// <summary>
    /// Sets whether the response should be stored for later retrieval. This corresponds to the "store" property in the JSON representation.
    /// </summary>
    public bool? StoredOutputEnabled { get; set; }
}