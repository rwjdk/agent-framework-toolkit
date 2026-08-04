using AgentFrameworkToolkit.Mistral;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mistral.SDK;
using Secrets;

namespace AgentFrameworkToolkit.Tests.PaidModels;

public sealed class MistralTests : TestsBase
{
    [Fact]
    public async Task EmbeddingFactory()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        MistralEmbeddingFactory factory = new(secrets.MistralApiKey);
        IEmbeddingGenerator<string, Embedding<float>> generator = factory.GetEmbeddingGenerator();
        Embedding<float> embedding = await generator.GenerateAsync("Hello", cancellationToken: TestContext.Current.CancellationToken,
            options: new EmbeddingGenerationOptions
            {
                ModelId = ModelDefinitions.MistralEmbed
            });
        Assert.Equal(1024, embedding.Dimensions);
    }

    [Fact]
    public Task AgentFactory_Simple() => SimpleAgentTestsAsync(AgentProvider.Mistral);

    [Fact]
    public Task AgentFactory_Normal() => NormalAgentTestsAsync(AgentProvider.Mistral);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.Mistral);

    [Fact]
    public Task AgentFactory_ToolCall() => ToolCallAgentTestsAsync(AgentProvider.Mistral);

    [Fact]
    public Task AgentFactory_McpToolCall() => McpToolCallAgentTestsAsync(AgentProvider.Mistral);

    [Fact]
    public async Task AgentFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddMistralAgentFactory(secrets.MistralApiKey);

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<MistralAgentFactory>()
            .CreateAgent(MistalChatModels.MistralSmall)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection_Connection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddMistralAgentFactory(new MistralConnection
        {
            ApiKey = secrets.MistralApiKey,
            NetworkTimeout = TimeSpan.FromSeconds(10)
        });

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<MistralAgentFactory>()
            .CreateAgent(MistalChatModels.MistralSmall)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }
}
