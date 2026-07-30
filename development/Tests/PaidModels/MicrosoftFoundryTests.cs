using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.MicrosoftFoundry;
using AgentFrameworkToolkit.OpenAI;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Secrets;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.Tests.PaidModels;

public sealed class MicrosoftFoundryTests : TestsBase
{
    [PublicAPI]
    private sealed class BatchStructuredReply
    {
        public required string Answer { get; set; }
    }

    [Fact]
    public Task AgentFactory_Simple_ChatClient() => SimpleAgentTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_Normal_ChatClient() => NormalAgentTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ChatClient() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_ToolCall_ChatClient() => ToolCallAgentTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_McpToolCall_ChatClient() => McpToolCallAgentTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_StructuredOutput_ChatClient() => StructuredOutputAgentTestsAsync(AgentProvider.MicrosoftFoundryChatClient);

    [Fact]
    public Task AgentFactory_Simple_ResponsesApi() => SimpleAgentTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public Task AgentFactory_Normal_ResponsesApi() => NormalAgentTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public Task AgentFactory_OpenTelemetryAndLoggingMiddleware_ResponsesApi() => OpenTelemetryAndLoggingMiddlewareTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public Task AgentFactory_ToolCall_ResponsesApi() => ToolCallAgentTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public Task AgentFactory_McpToolCall_ResponsesApi() => McpToolCallAgentTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public Task AgentFactory_StructuredOutput_ResponsesApi() => StructuredOutputAgentTestsAsync(AgentProvider.MicrosoftFoundryResponsesApi);

    [Fact]
    public async Task AgentFactory_DependencyInjection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddMicrosoftFoundryAgentFactory(secrets.MicrosoftFoundryEndpoint, new AzureCliCredential());

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<MicrosoftFoundryAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt5Nano)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task AgentFactory_DependencyInjection_Connection()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        ServiceCollection services = new();
        services.AddMicrosoftFoundryAgentFactory(new MicrosoftFoundryConnection
        {
            Endpoint = secrets.MicrosoftFoundryEndpoint,
            AuthenticationTokenProvider = new AzureCliCredential(),
            NetworkTimeout = TimeSpan.FromSeconds(10)
        });

        ServiceProvider provider = services.BuildServiceProvider();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string text = (await provider.GetRequiredService<MicrosoftFoundryAgentFactory>()
            .CreateAgent(OpenAIChatModels.Gpt5Nano)
            .RunAsync("Hello", cancellationToken: cancellationToken)).Text;
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task DeclarativeAgentFactory_CreateAgentAsync()
    {
        MicrosoftFoundryDeclarativeAgentFactory factory = CreateDeclarativeAgentFactory();
        string agentName = $"aft-test-{Guid.NewGuid():N}";
        MicrosoftFoundryAgent? createdAgent = null;

        try
        {
            createdAgent = factory.CreateAgent(
                agentName,
                OpenAIChatModels.Gpt5Nano,
                "Answer briefly.");

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string text = (await createdAgent.RunAsync("Reply with hello.", cancellationToken: cancellationToken)).Text;
            Assert.NotEmpty(text);

            MicrosoftFoundryAgent retrievedAgent = factory.GetAgent(agentName);
            Assert.Equal(agentName, retrievedAgent.Name);

            IList<MicrosoftFoundryAgent> agents = factory.GetAgents();
            Assert.Contains(agents, agent => agent.Name == agentName);
        }
        finally
        {
            if (createdAgent != null)
            {
                factory.DeleteAgent(agentName);
            }
        }
    }

    [Fact]
    public async Task DeclarativeAgentFactory_CreateAgentFromOptionsAsync()
    {
        MicrosoftFoundryDeclarativeAgentFactory factory = CreateDeclarativeAgentFactory();
        string agentName = $"aft-test-{Guid.NewGuid():N}";
        MicrosoftFoundryAgent? createdAgent = null;

        try
        {
            createdAgent = factory.CreateAgent(new DeclarativeAgentCreationOptions
            {
                Name = agentName,
                Model = OpenAIChatModels.Gpt5Nano,
                Instructions = "Answer briefly."
            });

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string text = (await createdAgent.RunAsync("Reply with hello.", cancellationToken: cancellationToken)).Text;
            Assert.NotEmpty(text);

            IList<ProjectsAgentVersion> versions = factory.GetAgentVersions(agentName);
            ProjectsAgentVersion version = Assert.Single(versions);
            Assert.Equal(agentName, version.Name);

            MicrosoftFoundryAgent retrievedAgent = factory.GetAgent(agentName, version.Version);
            Assert.Equal(agentName, retrievedAgent.Name);
        }
        finally
        {
            if (createdAgent != null)
            {
                factory.DeleteAgent(agentName);
            }
        }
    }

    [Fact]
    public async Task DeclarativeAgentFactory_CreateAgentFromDefinitionAsync()
    {
        MicrosoftFoundryDeclarativeAgentFactory factory = CreateDeclarativeAgentFactory();
        string agentName = $"aft-test-{Guid.NewGuid():N}";
        MicrosoftFoundryAgent? createdAgent = null;

        try
        {
            DeclarativeAgentDefinition definition = new(OpenAIChatModels.Gpt5Nano)
            {
                Instructions = "Answer briefly."
            };
            createdAgent = factory.CreateAgent(agentName, definition);

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string text = (await createdAgent.RunAsync("Reply with hello.", cancellationToken: cancellationToken)).Text;
            Assert.NotEmpty(text);
        }
        finally
        {
            if (createdAgent != null)
            {
                factory.DeleteAgent(agentName);
            }
        }
    }

    [Fact]
    public async Task DeclarativeAgentFactory_LocalToolCallAsync()
    {
        MicrosoftFoundryDeclarativeAgentFactory factory = CreateDeclarativeAgentFactory();
        string agentName = $"aft-test-{Guid.NewGuid():N}";
        IList<AITool> tools = [AIFunctionFactory.Create(EchoValue, "echo_value")];
        MicrosoftFoundryAgent? createdAgent = null;

        try
        {
            createdAgent = factory.CreateAgent(new DeclarativeAgentCreationOptions
            {
                Name = agentName,
                Model = OpenAIChatModels.Gpt5Nano,
                Instructions = "Always use the echo_value tool when asked to echo a value.",
                Tools = tools
            });

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string text = (await createdAgent.RunAsync(
                "Use echo_value to echo 'declarative-tool-result', then return only the tool result.",
                cancellationToken: cancellationToken)).Text;
            Assert.Contains("declarative-tool-result", text, StringComparison.OrdinalIgnoreCase);

            MicrosoftFoundryAgent retrievedAgent = factory.GetAgent(agentName, tools);
            text = (await retrievedAgent.RunAsync(
                "Use echo_value to echo 'retrieved-tool-result', then return only the tool result.",
                cancellationToken: cancellationToken)).Text;
            Assert.Contains("retrieved-tool-result", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (createdAgent != null)
            {
                factory.DeleteAgent(agentName);
            }
        }
    }

    private static string EchoValue(string value)
    {
        return value;
    }

    private static MicrosoftFoundryDeclarativeAgentFactory CreateDeclarativeAgentFactory()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        MicrosoftFoundryConnection connection = new(secrets.MicrosoftFoundryEndpoint, new AzureCliCredential());
        return new MicrosoftFoundryDeclarativeAgentFactory(connection);
    }
}
