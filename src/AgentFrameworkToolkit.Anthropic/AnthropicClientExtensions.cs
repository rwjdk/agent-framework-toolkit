using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Anthropic;

/// <summary>
/// Extensions for AnthropicClient
/// </summary>
public static class AnthropicClientExtensions
{
    /// <summary>
    /// Create an Agent
    /// </summary>
    /// <param name="client">AnthropicClient</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static AnthropicAgent AsAIAgent(this AnthropicClient client, AnthropicAgentOptions options)
    {
        ChatClientAgentOptions chatClientAgentOptions = AnthropicAgentFactory.CreateChatClientAgentOptions(options);
        AIAgent innerAgent = new ChatClientAgent(client.AsIChatClient(), chatClientAgentOptions,
            options.LoggerFactory,
            options.Services);

        return new AnthropicAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }
}