using Amazon.BedrockRuntime;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.AmazonBedrock;

/// <summary>
/// Extensions for IAmazonBedrockRuntime
/// </summary>
public static class AmazonBedrockRuntimeExtensions
{
    /// <summary>
    /// Create an Agent
    /// </summary>
    /// <param name="amazonBedrockRuntime">Runtime</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static AmazonBedrockAgent AsAIAgent(this IAmazonBedrockRuntime amazonBedrockRuntime, AmazonBedrockAgentOptions options)
    {
        IAmazonBedrockRuntime runtimeClient = amazonBedrockRuntime;
        IChatClient client = runtimeClient.AsIChatClient(options.Model);

        ChatClientAgentOptions chatClientAgentOptions = AmazonBedrockAgentFactory.CreateChatClientAgentOptions(options);

        AIAgent innerAgent = new ChatClientAgent(client, chatClientAgentOptions, options.LoggerFactory, options.Services);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (options.ToolCallingMiddleware != null || options.OpenTelemetryMiddleware != null || options.LoggingMiddleware != null)
        {
            return new AmazonBedrockAgent(MiddlewareHelper.ApplyMiddleware(
                innerAgent,
                null,
                options.ToolCallingMiddleware,
                options.OpenTelemetryMiddleware,
                options.LoggingMiddleware,
                options.Services));
        }

        return new AmazonBedrockAgent(innerAgent);
    }
}