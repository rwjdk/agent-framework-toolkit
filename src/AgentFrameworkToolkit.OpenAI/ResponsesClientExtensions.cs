using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// Extensions for Responses Client
/// </summary>
public static class ResponsesClientExtensions
{
    /// <summary>
    /// Create an Agent
    /// </summary>
    /// <param name="client">ResponsesAPI Client</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static AIAgent AsAIAgent(this ResponsesClient client, AgentOptions options)
    {
        ChatClientAgentOptions chatClientAgentOptions = OpenAIAgentFactory.CreateChatClientAgentOptions(options, ClientType.ResponsesApi);
        Func<IChatClient, IChatClient>? clientFactory = options.ClientFactory;
        ILoggerFactory? loggerFactory = options.LoggerFactory;
        IServiceProvider? services = options.Services;
        ChatClientAgent innerAgent = client.AsAIAgent(chatClientAgentOptions, options.Model, clientFactory, loggerFactory, services);
        return new OpenAIAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }
}