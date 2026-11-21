using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Secrets;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.Tests.Providers;

public sealed class AzureOpenAITests : TestsBase
{
    [PublicAPI]
    private sealed class BatchStructuredReply
    {
        public required string Answer { get; set; }
    }

    [Fact]
    public Task AgentFactory_Simple_ChatClient() => SimpleAgentTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_Normal_ChatClient() => NormalAgentTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ChatClient() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_ToolCall_ChatClient() => ToolCallAgentTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_McpToolCall_ChatClient() => McpToolCallAgentTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_StructuredOutput_ChatClient() => StructuredOutputAgentTestsAsync(AgentProvider.AzureOpenAIChatClient);

    [Fact]
    public Task AgentFactory_Simple_ResponsesApi() => SimpleAgentTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_Normal_ResponsesApi() => NormalAgentTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ResponsesApi() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_ToolCall_ResponsesApi() => ToolCallAgentTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_McpToolCall_ResponsesApi() => McpToolCallAgentTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_StructuredOutput_ResponsesApi() => StructuredOutputAgentTestsAsync(AgentProvider.AzureOpenAIResponsesApi);

    [Fact]
    public async Task AgentFactory_ImageGeneration_ResponsesApi()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIAgentFactory factory = new(new AzureOpenAIConnection
        {
            Endpoint = secrets.AzureOpenAiEndpoint,
            ApiKey = secrets.AzureOpenAiKey,
            DefaultClientType = ClientType.ResponsesApi,
            NetworkTimeout = TimeSpan.FromMinutes(5)
        });
        AzureOpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt6Luna,
            Tools = [new HostedImageGenerationTool
            {
                Options = new ImageGenerationOptions
                {
                    ModelId = "gpt-image-2",
                    MediaType = "image/png"
                }
            }]
        });

        AgentResponse response = await agent.RunAsync(
            "Create a simple image of a red circle on a white background.",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(
            response.Messages.SelectMany(message => message.Contents),
            content => content is ImageGenerationToolResultContent result &&
                       result.Outputs?.Any(output => output is DataContent data && !data.Data.IsEmpty) == true);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddAzureOpenAIAgentFactory(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<AzureOpenAIAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt6Luna)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection_Connection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddAzureOpenAIAgentFactory(new AzureOpenAIConnection
        {
            Endpoint = secrets.AzureOpenAiEndpoint,
            ApiKey = secrets.AzureOpenAiKey,
            NetworkTimeout = TimeSpan.FromSeconds(10)
        });

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<AzureOpenAIAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt6Luna)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task EmbeddingFactory()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIEmbeddingFactory factory = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);
        IEmbeddingGenerator<string, Embedding<float>> generator = factory.GetEmbeddingGenerator("text-embedding-3-small");
        Embedding<float> embedding = await generator.GenerateAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }

    [Fact]
    public async Task EmbeddingFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddAzureOpenAIEmbeddingFactory(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        AzureOpenAIEmbeddingFactory embeddingFactory = provider.GetRequiredService<AzureOpenAIEmbeddingFactory>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Embedding<float> embedding = await embeddingFactory.GetEmbeddingGenerator("text-embedding-3-small")
            .GenerateAsync("Hello", cancellationToken: cancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }
}
