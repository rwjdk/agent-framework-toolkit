using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Secrets;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.Tests.Providers;

public sealed class OpenAITests : TestsBase
{
    [PublicAPI]
    private sealed class BatchStructuredReply
    {
        public required string Answer { get; set; }
    }

    [Fact]
    public Task AgentFactory_Simple_ChatClient() => SimpleAgentTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_Normal_ChatClient() => NormalAgentTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ChatClient() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_ToolCall_ChatClient() => ToolCallAgentTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_McpToolCall_ChatClient() => McpToolCallAgentTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_StructuredOutput_ChatClient() => StructuredOutputAgentTestsAsync(AgentProvider.OpenAIChatClient);

    [Fact]
    public Task AgentFactory_Simple_ResponsesApi() => SimpleAgentTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_Normal_ResponsesApi() => NormalAgentTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ResponsesApi() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_ToolCall_ResponsesApi() => ToolCallAgentTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_McpToolCall_ResponsesApi() => McpToolCallAgentTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public Task AgentFactory_StructuredOutput_ResponsesApi() => StructuredOutputAgentTestsAsync(AgentProvider.OpenAIResponsesApi);

    [Fact]
    public async Task AgentFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenAIAgentFactory(secrets.OpenAiApiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<OpenAIAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt6Luna)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection_Connection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenAIAgentFactory(new OpenAIConnection
        {
            ApiKey = secrets.OpenAiApiKey,
            NetworkTimeout = TimeSpan.FromSeconds(10)
        });

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<OpenAIAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt6Luna)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task EmbeddingFactory()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenAIEmbeddingFactory factory = new(secrets.OpenAiApiKey);
        IEmbeddingGenerator<string, Embedding<float>> generator = factory.GetEmbeddingGenerator("text-embedding-3-small");
        Embedding<float> embedding = await generator.GenerateAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }

    [Fact]
    public async Task EmbeddingFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenAIEmbeddingFactory(secrets.OpenAiApiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        OpenAIEmbeddingFactory embeddingFactory = provider.GetRequiredService<OpenAIEmbeddingFactory>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Embedding<float> embedding = await embeddingFactory.GetEmbeddingGenerator("text-embedding-3-small")
            .GenerateAsync("Hello", cancellationToken: cancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }
}
