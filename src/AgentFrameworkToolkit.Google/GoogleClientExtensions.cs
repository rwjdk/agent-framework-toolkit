using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Google;

/// <summary>
/// Extensions for Google Client
/// </summary>
public static class GoogleClientExtensions
{
    /// <summary>
    /// Create an Agent
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static GoogleAgent AsAIAgent(this Client client, GoogleAgentOptions options)
    {
        ChatClientAgentOptions chatClientAgentOptions = GoogleAgentFactory.CreateChatClientAgentOptions(options);
        AIAgent innerAgent = new ChatClientAgent(client.AsIChatClient(options.Model), chatClientAgentOptions, options.LoggerFactory, options.Services);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (options.RawToolCallDetails != null || options.ToolCallingMiddleware != null || options.OpenTelemetryMiddleware != null || options.LoggingMiddleware != null)
        {
            return new GoogleAgent(MiddlewareHelper.ApplyMiddleware(
                innerAgent,
                options.RawToolCallDetails,
                options.ToolCallingMiddleware,
                options.OpenTelemetryMiddleware,
                options.LoggingMiddleware,
                options.Services));
        }

        return new GoogleAgent(innerAgent);
    }
}