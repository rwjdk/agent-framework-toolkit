using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Secrets;
#pragma warning disable OPENAI001
#pragma warning disable AFT999

namespace Sandbox.Providers;


class MyObject
{
    public required string City { get; set; }
    public required int PopulationInMillion { get; set; }
}

public static class AzureOpenAI
{
    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIConnection connection = new AzureOpenAIConnection
        {
            Endpoint = secrets.AzureOpenAiEndpoint,
            ApiKey = secrets.AzureOpenAiKey,
        };

        AIAgent a = connection.GetClient().AsAIAgent(new AgentOptions
        {
           Model = "gpt-5.6-luna",
        });

        AgentResponse agentResponse = await a.RunAsync("Hello");


        AzureOpenAIAgentFactory factory = new AzureOpenAIAgentFactory(connection);

        AzureOpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            Model = "gpt-5.6-luna",
            ReasoningEffort = OpenAIReasoningEffort.Low,
            ClientType = ClientType.ResponsesApi,
            
            RawToolCallDetails = Console.WriteLine
        });
        AgentSession session = await agent.CreateSessionAsync();

        AgentResponse response = await agent.RunAsync("What is the capital of France?", session);

        IList<ChatMessage> chatMessages = session.GetMessages();
    }

    public class MathResult
    {
        public required int Result { get; set; }
    }
}
