using AgentFrameworkToolkit.OpenRouter;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Secrets;

namespace AgentFrameworkToolkit.Tests.PaidModels;

public sealed class OpenRouterTests : TestsBase
{
    [Fact]
    public Task AgentFactory_Simple_ChatClient() => SimpleAgentTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_Normal_ChatClient() => NormalAgentTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ChatClient() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_ToolCall_ChatClient() => ToolCallAgentTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_ToolCall_ResponsesApi() => ToolCallAgentTestsAsync(AgentProvider.OpenRouterResponsesApi);
    
    [Fact]
    public Task AgentFactory_McpToolCall_ChatClient() => McpToolCallAgentTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_StructuredOutput_ChatClient() => StructuredOutputAgentTestsAsync(AgentProvider.OpenRouterChatClient);

    [Fact]
    public Task AgentFactory_Simple_ResponsesApi() => SimpleAgentTestsAsync(AgentProvider.OpenRouterResponsesApi);

    [Fact]
    public Task AgentFactory_Normal_ResponsesApi() => NormalAgentTestsAsync(AgentProvider.OpenRouterResponsesApi);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ResponsesApi() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.OpenRouterResponsesApi);

    [Fact]
    public Task AgentFactory_StructuredOutput_ResponsesApi() => StructuredOutputAgentTestsAsync(AgentProvider.OpenRouterResponsesApi);

    [Fact]
    public async Task AgentFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenRouterAgentFactory(secrets.OpenRouterApiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<OpenRouterAgentFactory>()
            .CreateAgent(OpenRouterChatModels.OpenAI.Gpt5Nano)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection_Connection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenRouterAgentFactory(new OpenRouterConnection
        {
            ApiKey = secrets.OpenRouterApiKey,
            NetworkTimeout = TimeSpan.FromSeconds(10)
        });

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<OpenRouterAgentFactory>()
            .CreateAgent(OpenRouterChatModels.OpenAI.Gpt5Nano)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task EmbeddingFactory()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenRouterEmbeddingFactory factory = new(secrets.OpenRouterApiKey);
        IEmbeddingGenerator<string, Embedding<float>> generator = factory.GetEmbeddingGenerator("openai/text-embedding-3-small");
        Embedding<float> embedding = await generator.GenerateAsync("Hello", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }

    [Fact]
    public async Task EmbeddingFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddOpenRouterEmbeddingFactory(secrets.OpenRouterApiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        OpenRouterEmbeddingFactory embeddingFactory = provider.GetRequiredService<OpenRouterEmbeddingFactory>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Embedding<float> embedding = await embeddingFactory.GetEmbeddingGenerator("openai/text-embedding-3-small")
            .GenerateAsync("Hello", cancellationToken: cancellationToken);
        Assert.Equal(1536, embedding.Dimensions);
    }
}
