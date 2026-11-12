using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenRouter;
using AgentFrameworkToolkit.Tools;
using AgentFrameworkToolkit.Tools.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Secrets;

namespace Sandbox.Providers;

public static class OpenRouter
{
    static string GetWeather(string city)
    {
        return "{ \"condition\": \"sunny\", \"degrees\":19 }";
    }

    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenRouterAgentFactory factory = new(new OpenRouterConnection
        {
            ApiKey = secrets.OpenRouterApiKey
        });
        
        OpenRouterAgent agent = factory.CreateAgent(new AgentOptions
        {
            Model = OpenRouterChatModels.OpenAI.Gpt41Nano,
        });

        AgentResponse response = await agent.RunAsync("What is the weather like in Paris?");
        Console.WriteLine(response);
    }
}
