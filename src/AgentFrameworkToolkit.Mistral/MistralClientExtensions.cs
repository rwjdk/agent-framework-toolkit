using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mistral.SDK;

namespace AgentFrameworkToolkit.Mistral;

/// <summary>
/// Extensions for MistralClient
/// </summary>
public static class MistralClientExtensions
{
    /// <summary>
    /// Create an Agent 
    /// </summary>
    /// <param name="client">MistralClient</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static MistralAgent AsAIAgent(this MistralClient client, MistralAgentOptions options)
    {
        AIAgent innerAgent = new ChatClientAgent(client.Completions, MistralAgentFactory.CreateChatClientAgentOptions(options), options.LoggerFactory, options.Services);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (options.RawToolCallDetails != null || options.ToolCallingMiddleware != null || options.OpenTelemetryMiddleware != null || options.LoggingMiddleware != null)
        {
            return new MistralAgent(MiddlewareHelper.ApplyMiddleware(
                innerAgent,
                options.RawToolCallDetails,
                options.ToolCallingMiddleware,
                options.OpenTelemetryMiddleware,
                options.LoggingMiddleware,
                options.Services));
        }

        return new MistralAgent(innerAgent);
    }
}