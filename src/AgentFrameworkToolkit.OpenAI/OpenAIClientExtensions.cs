using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// Extensions for Chat Client
/// </summary>
public static class OpenAIClientExtensions
{
    /// <summary>
    /// Create an Agent (using the ResponsesAPI unless specified)
    /// </summary>
    /// <param name="client">ChatClient</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static AIAgent AsAIAgent(this OpenAIClient client, AgentOptions options)
    {
        ClientType clientType = options.ClientType ?? ClientType.ResponsesApi;
        ChatClientAgent innerAgent = OpenAIAgentFactory.GetChatClientAgent(options, client, options.Model, clientType);
        return new OpenAIAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services), client, OpenAIDecisionFactory.ResolveAgentDecisionModel(options.Model, options.DecisionApiModel));
    }
}
