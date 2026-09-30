using AgentFrameworkToolkit.Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Agents.AI;
using Secrets;

namespace Sandbox.Providers;

public static class AnthropicDemo
{
    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();


        AnthropicAgentFactory agentFactory = new AnthropicAgentFactory(secrets.AnthropicApiKey);

        AnthropicAgent agent = agentFactory.CreateAgent(new AnthropicAgentOptions
        {
            Model = AnthropicChatModels.ClaudeFable5, //Pricing	$10 / $50 per MTok (input / output) [5.5 in comparison is 5$ / $30]
            //Model = AnthropicChatModels.ClaudeOpus48, 
            MaxOutputTokens = 10000,
            Effort = Effort.Medium,
            ServiceTier = ServiceTier.StandardOnly,
            UseAdaptiveThinking = false, //In fable this is always on regardless of setting
            RawHttpCallDetails = details =>
            {
                Console.WriteLine(details.RequestData);
                Console.WriteLine(details.ResponseData);
            }
        });

        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine() ?? "";
            AgentResponse response = await agent.RunAsync(input, session);
            Console.WriteLine(response);

            Console.WriteLine("------------------");
        }
    }
}
