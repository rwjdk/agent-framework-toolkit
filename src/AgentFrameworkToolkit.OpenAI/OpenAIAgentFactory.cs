using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// Factory for creating OpenAI Agents
/// </summary>
[PublicAPI]
public class OpenAIAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public OpenAIConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">Your OpenAI API Key (if you need a more advanced connection use the constructor overload)</param>
    public OpenAIAgentFactory(string apiKey)
    {
        Connection = new OpenAIConnection
        {
            ApiKey = apiKey
        };
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public OpenAIAgentFactory(OpenAIConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Create a simple Agent with default settings (For more advanced agents use the options overloads)
    /// </summary>
    /// <param name="model">Name of the Model to use</param>
    /// <param name="instructions">Instructions for the Agent to follow (aka Developer Message)</param>
    /// <param name="name">Name of the Agent</param>
    /// <param name="tools">Tools for the Agent</param>
    /// <returns>An Agent</returns>
    public OpenAIAgent CreateAgent(string model, string? instructions = null, string? name = null, IList<AITool>? tools = null)
    {
        return CreateAgent(new AgentOptions
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
    public OpenAIAgent CreateAgent(AgentOptions options)
    {
        OpenAIClient client = Connection.GetClient(options.RawHttpCallDetails);

        ChatClientAgent innerAgent = GetChatClientAgent(options, client, options.Model, Connection.DefaultClientType);
        return new OpenAIAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }

    internal static ChatClientAgent GetChatClientAgent(AgentOptions options, OpenAIClient client, string model, ClientType defaultClientType)
    {
        ChatClientAgentOptions chatClientAgentOptions = CreateChatClientAgentOptions(options, defaultClientType);
        Func<IChatClient, IChatClient>? clientFactory = options.ClientFactory;
        ILoggerFactory? loggerFactory = options.LoggerFactory;
        IServiceProvider? services = options.Services;
        return options.ClientType switch
        {
            ClientType.ChatClient => client.GetChatClient(model).AsAIAgent(chatClientAgentOptions, clientFactory, loggerFactory, services),
            ClientType.ResponsesApi => client.GetResponsesClient().AsAIAgent(chatClientAgentOptions, model, clientFactory, loggerFactory, services),
            null => defaultClientType switch
            {
                ClientType.ChatClient => client.GetChatClient(model).AsAIAgent(chatClientAgentOptions, clientFactory, loggerFactory, services),
                ClientType.ResponsesApi => client.GetResponsesClient().AsAIAgent(chatClientAgentOptions, model, clientFactory, loggerFactory, services),
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal static ChatClientAgentOptions CreateChatClientAgentOptions(AgentOptions options, ClientType defaultClientType)
    {
        bool anyOptionsSet = false;
        ChatOptions chatOptions = new();

        if (options.Tools != null)
        {
            anyOptionsSet = true;
            chatOptions.Tools = options.Tools;
        }

        if (options.MaxOutputTokens.HasValue)
        {
            anyOptionsSet = true;
            chatOptions.MaxOutputTokens = options.MaxOutputTokens.Value;
        }

        if (options.Temperature != null && !OpenAIChatModels.ReasoningModels.Contains(options.Model))
        {
            anyOptionsSet = true;
            chatOptions.Temperature = options.Temperature;
        }

        string? instructions = options.Instructions;
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            anyOptionsSet = true;
            chatOptions.Instructions = instructions;
        }

        string? reasoningEffortAsString = null;
        OpenAIReasoningEffort? effort = options.ReasoningEffort;
        switch (effort)
        {
            case OpenAIReasoningEffort.None:
                reasoningEffortAsString = "none";
                break;
            case OpenAIReasoningEffort.Minimal:
                reasoningEffortAsString = "minimal";
                break;
            case OpenAIReasoningEffort.Low:
                reasoningEffortAsString = "low";
                break;
            case OpenAIReasoningEffort.Medium:
                reasoningEffortAsString = "medium";
                break;
            case OpenAIReasoningEffort.High:
                reasoningEffortAsString = "high";
                break;
            case OpenAIReasoningEffort.ExtraHigh:
                reasoningEffortAsString = "xhigh";
                break;
        }

        ClientType clientType = options.ClientType ?? defaultClientType;
        switch (clientType)
        {
            case ClientType.ChatClient:
                {
                    bool? storedOutputEnabled = options.StoredOutputEnabled;
                    string? configuredReasoningEffort = !string.IsNullOrWhiteSpace(reasoningEffortAsString) &&
                                                        !OpenAIChatModels.NonReasoningModels.Contains(options.Model)
                        ? reasoningEffortAsString
                        : null;
                    ChatServiceTier? serviceTier = ChatClientServiceTierParser();

                    if (storedOutputEnabled.HasValue || configuredReasoningEffort != null || serviceTier != null)
                    {
                        anyOptionsSet = true;
                        chatOptions.RawRepresentationFactory = _ => new ChatCompletionOptions
                        {
                            StoredOutputEnabled = storedOutputEnabled,
                            ReasoningEffortLevel = configuredReasoningEffort != null
                                ? new ChatReasoningEffortLevel(configuredReasoningEffort)
                                : null,
                            ServiceTier = serviceTier
                        };
                    }
                    break;
                }
            case ClientType.ResponsesApi:
                {
                    bool? storedOutputEnabled = options.StoredOutputEnabled;
                    string? configuredReasoningEffort = !string.IsNullOrWhiteSpace(reasoningEffortAsString) &&
                                                        !OpenAIChatModels.NonReasoningModels.Contains(options.Model)
                        ? reasoningEffortAsString
                        : null;
                    ResponseReasoningSummaryVerbosity? reasoningSummaryVerbosity = ResponseReasonSummaryVerbosityParser();
                    ResponseServiceTier? serviceTier = ResponseServiceTierParser();

                    if (storedOutputEnabled.HasValue || configuredReasoningEffort != null || serviceTier != null)
                    {
                        anyOptionsSet = true;
                        chatOptions.RawRepresentationFactory = _ => new CreateResponseOptions
                        {
                            StoredOutputEnabled = storedOutputEnabled,
                            ReasoningOptions = configuredReasoningEffort != null
                                ? new ResponseReasoningOptions
                                {
                                    ReasoningEffortLevel = new ResponseReasoningEffortLevel(configuredReasoningEffort),
                                    ReasoningSummaryVerbosity = reasoningSummaryVerbosity
                                }
                                : null,
                            ServiceTier = serviceTier
                        };
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        ChatClientAgentOptions chatClientAgentOptions = new()
        {
            Name = options.Name,
            Description = options.Description,
            Id = options.Id,
            AIContextProviders = options.AIContextProviders,
            ChatHistoryProvider = options.ChatHistoryProvider,
        };

        if (anyOptionsSet)
        {
            chatClientAgentOptions.ChatOptions = chatOptions;
        }

        options.AdditionalChatClientAgentOptions?.Invoke(chatClientAgentOptions);

        return chatClientAgentOptions;

        ResponseServiceTier? ResponseServiceTierParser()
        {
            return options.ServiceTier switch
            {
                OpenAIServiceTier.Auto => new ResponseServiceTier("auto"),
                OpenAIServiceTier.Flex => new ResponseServiceTier("flex"),
                OpenAIServiceTier.Default => new ResponseServiceTier("default"),
                OpenAIServiceTier.Priority => new ResponseServiceTier("priority"),
                null => (ResponseServiceTier?)null,
                _ => throw new ArgumentOutOfRangeException(nameof(options.ServiceTier), options.ServiceTier, null)
            };
        }

        ChatServiceTier? ChatClientServiceTierParser()
        {
            return options.ServiceTier switch
            {
                OpenAIServiceTier.Auto => new ChatServiceTier("auto"),
                OpenAIServiceTier.Flex => new ChatServiceTier("flex"),
                OpenAIServiceTier.Default => new ChatServiceTier("default"),
                OpenAIServiceTier.Priority => new ChatServiceTier("priority"),
                null => (ChatServiceTier?)null,
                _ => throw new ArgumentOutOfRangeException(nameof(options.ServiceTier), options.ServiceTier, null)
            };
        }

        ResponseReasoningSummaryVerbosity? ResponseReasonSummaryVerbosityParser()
        {
            return options.ReasoningSummaryVerbosity switch
            {
                OpenAIReasoningSummaryVerbosity.Auto => ResponseReasoningSummaryVerbosity.Auto,
                OpenAIReasoningSummaryVerbosity.Concise => ResponseReasoningSummaryVerbosity.Concise,
                OpenAIReasoningSummaryVerbosity.Detailed => ResponseReasoningSummaryVerbosity.Detailed,
                null => (ResponseReasoningSummaryVerbosity?)null,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
