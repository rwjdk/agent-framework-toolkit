using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Creation Options for a Declarative Agent
/// </summary>
[PublicAPI]
public class DeclarativeAgentCreationOptions : DeclarativeAgentOptions
{
    /// <summary>
    /// Model to use
    /// </summary>
    public required string Model { get; set; }
    
    /// <summary>
    /// Instruction for the Agent to be fed to the LLM as System/Developer Message
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// A set of Tools that the Agent are allowed to call
    /// </summary>
    public IList<AITool>? Tools { get; set; }

    /// <summary>
    /// Reasoning Effort to use
    /// </summary>
    public ResponseReasoningEffortLevel? ReasoningEffort { get; set; }

    /// <summary>
    /// Reasoning Summary Verbosity
    /// </summary>
    public ResponseReasoningSummaryVerbosity? ReasoningSummaryVerbosity { get; set; }

    /// <summary>
    /// Determine if the WebSearch Tools should be available for the Agent (Default: false)
    /// </summary>
    public bool WebSearchTool { get; set; }

    /// <summary>
    /// Determine if the Code Interpreter Tool should be available for the Agent (Default: false)
    /// </summary>
    public bool CodeInterpreterTool { get; set; }

    /// <summary>
    /// MCP tools you wish to give to the agent
    /// </summary>
    public IList<McpTool>? McpTools { get; set; } = [];
}