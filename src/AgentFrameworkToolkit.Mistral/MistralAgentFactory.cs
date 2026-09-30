using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Mistral;

/// <summary>
/// Factory for creating Mistral Agents
/// </summary>
[PublicAPI]
public class MistralAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public MistralConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">Your Mistral API Key (if you need a more advanced connection use the constructor overload)</param>
    public MistralAgentFactory(string apiKey)
    {
        Connection = new MistralConnection
        {
            ApiKey = apiKey
        };
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public MistralAgentFactory(MistralConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Create a simple Agent (using the ChatClient) with default settings (For more advanced agents use the options overloads)
    /// </summary>
    /// <param name="model">Name of the Model to use</param>
    /// <param name="instructions">Instructions for the Agent to follow (aka Developer Message)</param>
    /// <param name="name">Name of the Agent</param>
    /// <param name="tools">Tools for the Agent</param>
    /// <returns>An Agent</returns>
    public MistralAgent CreateAgent(string model, string? instructions = null, string? name = null, IList<AITool>? tools = null)
    {
        return CreateAgent(new MistralAgentOptions
        {
            Model = model,
            Name = name,
            Instructions = instructions,
            Tools = tools
        });
    }

    /// <summary>
    /// Create a new Agent
    /// </summary>
    /// <param name="options">Options for the agent</param>
    /// <returns>The Agent</returns>
    public MistralAgent CreateAgent(MistralAgentOptions options)
    {
        IChatClient client = Connection.GetClient(options.RawHttpCallDetails).Completions;

        AIAgent innerAgent = new ChatClientAgent(client, CreateChatClientAgentOptions(options), options.LoggerFactory, options.Services);

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

    internal static ChatClientAgentOptions CreateChatClientAgentOptions(MistralAgentOptions options)
    {
        ChatOptions chatOptions = new()
        {
            ModelId = options.Model,
            Instructions = options.Instructions
        };

        if (options.Tools != null)
        {
            chatOptions.Tools = options.Tools;
        }

        chatOptions.MaxOutputTokens = options.MaxOutputTokens;

        if (options.Temperature != null)
        {
            chatOptions.Temperature = options.Temperature;
        }

        ChatClientAgentOptions chatClientAgentOptions = new()
        {
            Name = options.Name,
            Description = options.Description,
            Id = options.Id,
            ChatOptions = chatOptions,
            AIContextProviders = options.AIContextProviders,
            ChatHistoryProvider = options.ChatHistoryProvider,
        };

        options.AdditionalChatClientAgentOptions?.Invoke(chatClientAgentOptions);

        return chatClientAgentOptions;
    }
}
