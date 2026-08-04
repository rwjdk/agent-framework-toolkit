using Amazon;
using AgentFrameworkToolkit.AmazonBedrock;
using AgentFrameworkToolkit.Anthropic;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.Cerebras;
using AgentFrameworkToolkit.Cohere;
using AgentFrameworkToolkit.Google;
using AgentFrameworkToolkit.Groq;
using AgentFrameworkToolkit.Mistral;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenRouter;
using AgentFrameworkToolkit.Tools;
using AgentFrameworkToolkit.Tools.ModelContextProtocol;
using AgentFrameworkToolkit.XAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Secrets;
using System.Diagnostics;
using AgentFrameworkToolkit.MicrosoftFoundry;
using Azure.Identity;
using JetBrains.Annotations;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.Tests;

public abstract class TestsBase
{
    protected TracerProvider? TracerProvider;
    protected string? ToolCallingMiddlewareCity;
    private string? _openTelemetryDisplayName;
    private McpClientTools? _mcpClientTools;
    private const string TestName = "MyAgent";
    private const string TestInstructions = "You are a nice AI and a weather expert";
    private const string TestDescription = "Sampledescription";

    static string GetWeatherWithServiceDependency(IServiceProvider serviceProvider, string city)
    {
        // ReSharper disable once UnusedVariable
        MyDi service = serviceProvider.GetRequiredService<MyDi>();
        return "{ \"condition\": \"sunny\", \"degrees\":19 }";
    }

    static string GetWeather(string city)
    {
        return "{ \"condition\": \"sunny\", \"degrees\":19 }";
    }

    protected async Task SimpleAgentTestsAsync(AgentProvider provider)
    {
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.Simple);
        Assert.NotNull(agent.Id);
        Assert.Equal(TestName, agent.Name);
        AgentResponse response = await agent.RunAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Messages);
        Assert.NotEmpty(response.Text);
    }

    protected async Task NormalAgentTestsAsync(AgentProvider provider)
    {
        TestLoggerFactory testLogger = new();
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.Normal, testLogger);
        Assert.NotNull(agent.Id);
        Assert.Equal(TestName, agent.Name);
        Assert.Equal(TestDescription, agent.Description);
        AgentResponse response = await agent.RunAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Messages);
        Assert.NotEmpty(response.Text);
        bool condition = testLogger.Logger.Messages.Any(x => x.Contains(agent.Id));
        Assert.True(condition);

        switch (provider)
        {
            case AgentProvider.AzureOpenAIChatClient:
            case AgentProvider.AzureOpenAIResponsesApi:
            case AgentProvider.OpenAIChatClient:
            case AgentProvider.OpenAIResponsesApi:
            case AgentProvider.OpenRouterChatClient:
            case AgentProvider.OpenRouterResponsesApi:
            case AgentProvider.CerebrasChatClient:
            case AgentProvider.CohereChatClient:
            case AgentProvider.GroqChatClient:
            case AgentProvider.GroqResponsesApi:
            case AgentProvider.XAIChatClient:
            case AgentProvider.XAIResponsesApi:
                Assert.Contains("ClientFactory Called", testLogger.Logger.Messages);
                break;
        }
    }

    protected async Task OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider provider)
    {
        _openTelemetryDisplayName = null;
        TestLoggerFactory testLogger = new();
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.OpenTelemetryAndLoggingMiddleware, testLogger);
        Assert.NotNull(agent.Id);
        Assert.Equal(TestName, agent.Name);
        Assert.Equal(TestDescription, agent.Description);
        AgentResponse response = await agent.RunAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Messages);
        Assert.NotEmpty(response.Text);
        bool idCondition = testLogger.Logger.Messages.Any(x => x.Contains(agent.Id));
        bool completedCondition = testLogger.Logger.Messages.Any(x => x.StartsWith("RunAsync completed"));
        Assert.True(idCondition);
        Assert.True(completedCondition);
        TracerProvider?.ForceFlush();
        Assert.Equal($"invoke_agent {agent.Name}({agent.Id})", _openTelemetryDisplayName);

        switch (provider)
        {
            case AgentProvider.AzureOpenAIChatClient:
            case AgentProvider.AzureOpenAIResponsesApi:
            case AgentProvider.OpenAIChatClient:
            case AgentProvider.OpenAIResponsesApi:
            case AgentProvider.OpenRouterChatClient:
            case AgentProvider.OpenRouterResponsesApi:
            case AgentProvider.CerebrasChatClient:
            case AgentProvider.CohereChatClient:
            case AgentProvider.GroqChatClient:
            case AgentProvider.GroqResponsesApi:
            case AgentProvider.XAIChatClient:
            case AgentProvider.XAIResponsesApi:
                Assert.Contains("ClientFactory Called", testLogger.Logger.Messages);
                break;
        }
    }

    protected async Task ToolCallAgentTestsAsync(AgentProvider provider)
    {
        TestLoggerFactory testLogger = new();
        ToolCallingMiddlewareCity = null;
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.ToolCall, testLogger);
        AgentResponse response = await agent.RunAsync("What is the weather like in Paris", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Messages.Where(x => x.Role == ChatRole.Tool).ToList());
        Assert.Equal(3, response.Messages.Count);

        Assert.Contains("SUNNY", response.Text.ToUpperInvariant());
        Assert.Contains("19", response.Text);
        Assert.Equal("PARIS", ToolCallingMiddlewareCity?.ToUpperInvariant());
    }

    protected async Task McpToolCallAgentTestsAsync(AgentProvider provider)
    {
        TestLoggerFactory testLogger = new();
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.McpToolCall, testLogger);
        AgentResponse response = await agent.RunAsync("Call the 'getting_started' tool to find what URL the nuget is on", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.Messages.Count(x => x.Role == ChatRole.Tool) > 0);
        Assert.Contains("www.nuget.org/packages/TrelloDotNet".ToUpperInvariant(), response.Text.ToUpperInvariant());
    }

    protected async Task StructuredOutputAgentTestsAsync(AgentProvider provider)
    {
        TestLoggerFactory testLogger = new();
        Agent agent = await GetAgentForScenarioAsync(provider, AgentScenario.Normal, testLogger);
        AgentResponse<MovieResult> response = await agent.RunAsync<MovieResult>("Top 3 IMDB Movies", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, response.Result.Movies.Count);
    }
    
    [UsedImplicitly]
    private record MovieResult(List<Movie> Movies);

    [UsedImplicitly]
    private record Movie(string Title, int YearOfRelease);

    private async Task<Agent> GetAgentForScenarioAsync(AgentProvider provider, AgentScenario scenario, TestLoggerFactory? testLogger = null)
    {
        string sourceName = "AiSource";
        TracerProviderBuilder tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddProcessor(new BatchActivityExportProcessor(new CustomOpenTelemetryExporter(name => _openTelemetryDisplayName = name)));
        TracerProvider = tracerProviderBuilder.Build();

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton<MyDi>();
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        Func<IChatClient, IChatClient> clientFactory = client =>
        {
            testLogger?.Logger.Log(LogLevel.Debug, "ClientFactory Called");
            return client;
        };

        Secrets.Secrets secrets = SecretsManager.GetSecrets();

        IList<AITool> tools = await GetToolsForScenarioAsync(provider, scenario);
        switch (provider)
        {
            case AgentProvider.AzureOpenAIChatClient:
            {
                AzureOpenAIAgentFactory factory = new(new AzureOpenAIConnection
                {
                    Endpoint = secrets.AzureOpenAiEndpoint,
                    ApiKey = secrets.AzureOpenAiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, secrets.AzureOpenAiEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.AzureOpenAIResponsesApi:
            {
                AzureOpenAIAgentFactory factory = new(new AzureOpenAIConnection
                {
                    Endpoint = secrets.AzureOpenAiEndpoint,
                    ApiKey = secrets.AzureOpenAiKey,
                    DefaultClientType = ClientType.ResponsesApi
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, secrets.AzureOpenAiEndpoint, ClientType.ResponsesApi)),
                };
            }
            case AgentProvider.OpenAIChatClient:
            {
                OpenAIAgentFactory factory = new(new OpenAIConnection
                {
                    ApiKey = secrets.OpenAiApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, null, ClientType.ChatClient)),
                };
            }
            case AgentProvider.OpenAIResponsesApi:
            {
                OpenAIAgentFactory factory = new(new OpenAIConnection
                {
                    ApiKey = secrets.OpenAiApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, null, ClientType.ResponsesApi)),
                };
            }
            case AgentProvider.Anthropic:
            {
                AnthropicAgentFactory factory = new(secrets.AnthropicApiKey);
                string model = AnthropicChatModels.ClaudeHaiku45;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, 2000, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetAnthropicOptions(model)),
                };
            }
            case AgentProvider.Google:
            {
                GoogleAgentFactory factory = new(secrets.GoogleGeminiApiKey);
                string model = GoogleChatModels.Gemini25Flash;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetGoogleAgentOptions(model)),
                };
            }
            case AgentProvider.Mistral:
            {
                MistralAgentFactory factory = new(secrets.MistralApiKey);
                string model = "mistral-small-2603";
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetMistralAgentOptions(model)),
                };
            }
            case AgentProvider.AmazonBedrock:
            {
                AmazonBedrockAgentFactory factory = new(new AmazonBedrockConnection
                {
                    Region = RegionEndpoint.EUNorth1,
                    ApiKey = secrets.AmazonBedrockApiKey
                });
                string model = "eu.anthropic.claude-haiku-4-5-20251001-v1:0";
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetAmazonBedrockAgentOptions(model)),
                };
            }
            case AgentProvider.OpenRouterChatClient:
            {
                OpenRouterAgentFactory factory = new(new OpenRouterConnection
                {
                    ApiKey = secrets.OpenRouterApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = OpenRouterChatModels.OpenAI.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, OpenRouterConnection.DefaultEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.OpenRouterResponsesApi:
            {
                OpenRouterAgentFactory factory = new(new OpenRouterConnection
                {
                    ApiKey = secrets.OpenRouterApiKey,
                    DefaultClientType = ClientType.ResponsesApi
                });
                string model = OpenRouterChatModels.OpenAI.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, OpenRouterConnection.DefaultEndpoint, ClientType.ResponsesApi)),
                };
            }
            case AgentProvider.CerebrasChatClient:
            {
                CerebrasAgentFactory factory = new(new CerebrasConnection
                {
                    ApiKey = secrets.CerebrasApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = "qwen-3-235b-a22b-instruct-2507";
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, CerebrasConnection.DefaultEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.CohereChatClient:
            {
                CohereAgentFactory factory = new(new CohereConnection
                {
                    ApiKey = secrets.CohereApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = "command-a-03-2025";
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, CohereConnection.DefaultEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.GroqChatClient:
            {
                GroqAgentFactory factory = new(new GroqConnection
                {
                    ApiKey = secrets.GroqApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = GroqChatModels.GptOss20B;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, GroqConnection.DefaultEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.GroqResponsesApi:
            {
                GroqAgentFactory factory = new(new GroqConnection
                {
                    ApiKey = secrets.GroqApiKey,
                    DefaultClientType = ClientType.ResponsesApi
                });
                string model = GroqChatModels.GptOss20B;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, GroqConnection.DefaultEndpoint, ClientType.ResponsesApi)),
                };
            }
            case AgentProvider.XAIChatClient:
            {
                XAIAgentFactory factory = new(new XAIConnection
                {
                    ApiKey = secrets.XAiGrokApiKey,
                    DefaultClientType = ClientType.ChatClient
                });
                string model = XAIChatModels.Grok41FastNonReasoning;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, XAIConnection.DefaultEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.XAIResponsesApi:
            {
                XAIAgentFactory factory = new(new XAIConnection
                {
                    ApiKey = secrets.XAiGrokApiKey,
                    DefaultClientType = ClientType.ResponsesApi
                });
                string model = XAIChatModels.Grok41FastNonReasoning;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, XAIConnection.DefaultEndpoint, ClientType.ResponsesApi)),
                };
            }
            case AgentProvider.MicrosoftFoundryChatClient:
            {
                MicrosoftFoundryAgentFactory factory = new(new MicrosoftFoundryConnection
                {
                    Endpoint = secrets.MicrosoftFoundryEndpoint,
                    AuthenticationTokenProvider = new AzureCliCredential(),
                    DefaultClientType = ClientType.ChatClient
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, secrets.MicrosoftFoundryEndpoint, ClientType.ChatClient)),
                };
            }
            case AgentProvider.MicrosoftFoundryResponsesApi:
            {
                MicrosoftFoundryAgentFactory factory = new(new MicrosoftFoundryConnection
                {
                    Endpoint = secrets.MicrosoftFoundryEndpoint,
                    AuthenticationTokenProvider = new AzureCliCredential(),
                    DefaultClientType = ClientType.ResponsesApi
                });
                string model = OpenAIChatModels.Gpt5Nano;
                return scenario switch
                {
                    AgentScenario.Simple => factory.CreateAgent(model, TestInstructions, TestName, tools),
                    _ => factory.CreateAgent(await GetOpenAiBasedAgentOptions(model, secrets.MicrosoftFoundryEndpoint, ClientType.ResponsesApi)),
                };
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        Task<AgentOptions> GetOpenAiBasedAgentOptions(string model, string? endpoint, ClientType clientType)
        {
            bool assertReasoning = false;
            AgentOptions options = new()
            {
                ClientType = clientType,
                Model = model,
                Description = TestDescription,
                Name = TestName,
                MaxOutputTokens = 2000,
                Instructions = TestInstructions,
                Tools = tools,
                Services = serviceProvider,
                LoggerFactory = testLogger,
                ClientFactory = clientFactory,
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                RawHttpCallDetails = details =>
                {
                    if (endpoint != null)
                    {
                        Assert.Contains(endpoint, details.RequestUrl);
                    }

                    switch (provider)
                    {
                        case AgentProvider.AzureOpenAIChatClient:
                        case AgentProvider.OpenAIChatClient:
                        case AgentProvider.OpenRouterChatClient:
                        case AgentProvider.CerebrasChatClient:
                        case AgentProvider.CohereChatClient:
                        case AgentProvider.GroqChatClient:
                        case AgentProvider.XAIChatClient:
                        case AgentProvider.MicrosoftFoundryChatClient:
                            Assert.Contains("\"max_completion_tokens\": 2000", details.RequestData);
                            // ReSharper disable once AccessToModifiedClosure
                            if (assertReasoning)
                            {
                                Assert.Contains("\"reasoning_effort\": \"low\"", details.RequestData);
                            }

                            break;
                        case AgentProvider.AzureOpenAIResponsesApi:
                        case AgentProvider.OpenAIResponsesApi:
                        case AgentProvider.OpenRouterResponsesApi:
                        case AgentProvider.GroqResponsesApi:
                        case AgentProvider.XAIResponsesApi:
                        case AgentProvider.MicrosoftFoundryResponsesApi:
                            Assert.Contains("\"max_output_tokens\": 2000", details.RequestData);
                            // ReSharper disable once AccessToModifiedClosure
                            if (assertReasoning)
                            {
                                Assert.Contains("\"effort\": \"low\"", details.RequestData);
                                Assert.Contains("\"summary\": \"detailed\"", details.RequestData);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
                    }

                    Assert.Contains(model, details.RequestData);
                    Assert.Contains(TestInstructions, details.RequestData);
                    Assert.Contains($"\"model\": \"{model}\"", details.RequestData);
                    foreach (AITool tool in tools)
                    {
                        Assert.Contains($"\"name\": \"{tool.Name}\"", details.RequestData);
                    }
                },
                RawToolCallDetails = Console.WriteLine,
                ToolCallingMiddleware = async (_, context, next, token) =>
                {
                    if (scenario is AgentScenario.ToolCall)
                    {
                        Assert.True(context.Arguments.ContainsKey("city") && context.Arguments["city"]!.ToString() == "Paris");
                        ToolCallingMiddlewareCity = context.Arguments["city"]!.ToString();
                    }

                    return await next(context, token);
                },
                OpenTelemetryMiddleware = new OpenTelemetryMiddleware(sourceName, agent => agent.EnableSensitiveData = true),
                LoggingMiddleware = new LoggingMiddleware(testLogger)
            };

            switch (model)
            {
                case OpenAIChatModels.Gpt5Nano:
                case OpenRouterChatModels.OpenAI.Gpt5Nano:
                    options.ReasoningEffort = OpenAIReasoningEffort.Low;
                    options.ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed;
                    assertReasoning = true;
                    break;
                default:
                    options.Temperature = 0;
                    break;
            }

            return Task.FromResult(options);
        }

        Task<AnthropicAgentOptions> GetAnthropicOptions(string model)
        {
            return Task.FromResult(new AnthropicAgentOptions
            {
                Model = model,
                Name = TestName,
                Description = TestDescription,
                MaxOutputTokens = 2000,
                Tools = tools,
                Instructions = TestInstructions,
                BudgetTokens = 1024,
                LoggerFactory = testLogger,
                Services = serviceProvider,
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                RawHttpCallDetails = details =>
                {
                    Assert.Contains(model, details.RequestData);
                    Assert.Contains("\"max_tokens\": 2000", details.RequestData);
                    Assert.Contains("\"budget_tokens\": 1024", details.RequestData);
                    Assert.Contains(TestInstructions, details.RequestData);
                    Assert.Contains($"\"model\": \"{model}\"", details.RequestData);
                    foreach (AITool tool in tools)
                    {
                        Assert.Contains($"\"name\": \"{tool.Name}\"", details.RequestData);
                    }
                },
                ToolCallingMiddleware = async (_, context, next, token) =>
                {
                    if (scenario is AgentScenario.ToolCall)
                    {
                        Assert.True(context.Arguments.ContainsKey("city") && context.Arguments["city"]!.ToString() == "Paris");
                        ToolCallingMiddlewareCity = context.Arguments["city"]!.ToString();
                    }

                    return await next(context, token);
                },
                OpenTelemetryMiddleware = new OpenTelemetryMiddleware(sourceName, agent => agent.EnableSensitiveData = true),
                LoggingMiddleware = new LoggingMiddleware(testLogger)
            });
        }

        Task<GoogleAgentOptions> GetGoogleAgentOptions(string model)
        {
            return Task.FromResult(new GoogleAgentOptions
            {
                Model = model,
                MaxOutputTokens = 2000,
                Description = TestDescription,
                Name = TestName,
                Tools = tools,
                LoggerFactory = testLogger,
                Instructions = TestInstructions,
                Services = serviceProvider,
                ToolCallingMiddleware = async (_, context, next, token) =>
                {
                    if (scenario is AgentScenario.ToolCall)
                    {
                        Assert.True(context.Arguments.ContainsKey("city") && context.Arguments["city"]!.ToString() == "Paris");
                        ToolCallingMiddlewareCity = context.Arguments["city"]!.ToString();
                    }

                    return await next(context, token);
                },
                RawHttpCallDetails = details =>
                {
                    Assert.True(details.RequestUrl.Contains("googleapis.com", StringComparison.InvariantCultureIgnoreCase));
                    Assert.True(details.RequestData.Contains("parts", StringComparison.InvariantCultureIgnoreCase));
                    Assert.True(details.ResponseData.Contains("parts", StringComparison.InvariantCultureIgnoreCase));
                },
                OpenTelemetryMiddleware = new OpenTelemetryMiddleware(sourceName, agent => agent.EnableSensitiveData = true),
                LoggingMiddleware = new LoggingMiddleware(testLogger)
            });
        }

        Task<MistralAgentOptions> GetMistralAgentOptions(string model)
        {
            return Task.FromResult(new MistralAgentOptions
            {
                Model = model,
                Name = TestName,
                MaxOutputTokens = 2000,
                Description = TestDescription,
                Tools = tools,
                Services = serviceProvider,
                Instructions = TestInstructions,
                LoggerFactory = testLogger,
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                RawHttpCallDetails = details =>
                {
                    Assert.Contains(model, details.RequestData);
                    Assert.Contains("\"max_tokens\": 2000,", details.RequestData);
                    Assert.Contains(TestInstructions, details.RequestData);
                    Assert.Contains($"\"model\": \"{model}\"", details.RequestData);
                    foreach (AITool tool in tools)
                    {
                        Assert.Contains($"\"name\": \"{tool.Name}\"", details.RequestData);
                    }
                },
                ToolCallingMiddleware = async (_, context, next, token) =>
                {
                    if (scenario is AgentScenario.ToolCall)
                    {
                        Assert.True(context.Arguments.ContainsKey("city") && context.Arguments["city"]!.ToString() == "Paris");
                        ToolCallingMiddlewareCity = context.Arguments["city"]!.ToString();
                    }

                    return await next(context, token);
                },
                OpenTelemetryMiddleware = new OpenTelemetryMiddleware(sourceName, agent => agent.EnableSensitiveData = true),
                LoggingMiddleware = new LoggingMiddleware(testLogger)
            });
        }

        Task<AmazonBedrockAgentOptions> GetAmazonBedrockAgentOptions(string model)
        {
            return Task.FromResult(new AmazonBedrockAgentOptions
            {
                Model = model,
                Name = TestName,
                MaxOutputTokens = 2000,
                Description = TestDescription,
                Tools = tools,
                Services = serviceProvider,
                Instructions = TestInstructions,
                LoggerFactory = testLogger,
                ToolCallingMiddleware = async (_, context, next, token) =>
                {
                    if (scenario is AgentScenario.ToolCall)
                    {
                        Assert.True(context.Arguments.ContainsKey("city") && context.Arguments["city"]!.ToString() == "Paris");
                        ToolCallingMiddlewareCity = context.Arguments["city"]!.ToString();
                    }

                    return await next(context, token);
                },
                OpenTelemetryMiddleware = new OpenTelemetryMiddleware(sourceName, agent => agent.EnableSensitiveData = true),
                LoggingMiddleware = new LoggingMiddleware(testLogger)
            });
        }
    }

    private async Task<IList<AITool>> GetToolsForScenarioAsync(AgentProvider provider, AgentScenario scenario)
    {
        List<AITool> tools = [];
        switch (scenario)
        {
            case AgentScenario.ToolCall:
                switch (provider)
                {
                    case AgentProvider.CohereChatClient:
                        tools = [AIFunctionFactory.Create(GetWeather, "get_weather")];
                        break;
                    default:
                        tools = [AIFunctionFactory.Create(GetWeatherWithServiceDependency, "get_weather")];
                        break;
                }

                break;
            case AgentScenario.McpToolCall:
                _mcpClientTools ??= await new AIToolsFactory()
                    .GetToolsFromRemoteMcpAsync("https://trellodotnetassistantbackend.azurewebsites.net/runtime/webhooks/mcp?code=Tools");
                return _mcpClientTools.Tools;
        }

        return tools;
    }
}

internal class MyDi
{
    //Empty
}

public enum AgentScenario
{
    Simple,
    Normal,
    ToolCall,
    McpToolCall,
    OpenTelemetryAndLoggingMiddleware,
}

public enum AgentProvider
{
    AzureOpenAIChatClient,
    AzureOpenAIResponsesApi,
    OpenAIChatClient,
    OpenAIResponsesApi,
    Anthropic,
    Google,
    Mistral,
    AmazonBedrock,
    OpenRouterChatClient,
    OpenRouterResponsesApi,
    CerebrasChatClient,
    CohereChatClient,
    GroqChatClient,
    GroqResponsesApi,
    XAIChatClient,
    XAIResponsesApi,
    MicrosoftFoundryChatClient,
    MicrosoftFoundryResponsesApi,
}

public sealed class CustomOpenTelemetryExporter : BaseExporter<Activity>
{
    private readonly Action<string> _onExport;

    public CustomOpenTelemetryExporter(Action<string> onExport)
    {
        ArgumentNullException.ThrowIfNull(onExport);
        _onExport = onExport;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (Activity activity in batch)
        {
            _onExport(activity.DisplayName);
            // Send to your destination (HTTP, queue, DB, etc.)
        }

        return ExportResult.Success;
    }
}
