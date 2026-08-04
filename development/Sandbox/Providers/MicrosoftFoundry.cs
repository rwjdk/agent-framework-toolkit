using AgentFrameworkToolkit.MicrosoftFoundry;
using AgentFrameworkToolkit.OpenAI;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Google.GenAI;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using Secrets;
using System.ClientModel;
using AgentFrameworkToolkit;
using AgentFrameworkToolkit.Tools.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

// ReSharper disable MethodHasAsyncOverload

#pragma warning disable OPENAI001

namespace Sandbox.Providers;

public static class MicrosoftFoundry
{
    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        string endpoint = secrets.MicrosoftFoundryEndpoint;
        MicrosoftFoundryConnection connection = new(endpoint, new AzureCliCredential());
        MicrosoftFoundryAgentFactory factory = new(connection);
        /*
        MicrosoftFoundryAgent agent = factory.HostedAgentFactory.CreateAgent(new HostedAgentCreationOptions
        {
            DotNetRuntime = "dotnet_10",
            Cpu = "0.5",
            Memory = "1Gi",
            SourceDirectory = @"C:\Users\rasmu\AppData\Local\Temp\AgentFrameworkToolkit\MyHostedAgent",
            AssemblyName = "MyHostedAgent.dll",
            Name = "Hosted",
        });
        */
        MicrosoftFoundryAgent agent = factory.HostedAgentFactory.GetAgent("Hosted");

        AgentResponse response = await agent.RunAsync("What is the Weather like in paris?");


        MicrosoftFoundryAgent agent2 = factory.DeclarativeAgentFactory.CreateAgent(new DeclarativeAgentCreationOptions
        {
            Name = "MyCoolAgent",
            Model = "gpt-5.6-luna",
            Instructions = "Speak like a pirate",
            ReasoningEffort = ResponseReasoningEffortLevel.Low,
            WebSearchTool = true,
            CodeInterpreterTool = true,
            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed,
            Tools = [TimeTools.GetNowLocal()],

            RawToolCallDetails = details =>
            {
                Console.WriteLine(details.ToString());
            }
        });

        AgentResponse agentResponse = await agent2.RunAsync("What is the time?");
        Console.WriteLine(agentResponse);
        /*
        IList<MicrosoftFoundryAgent> agents = factory.DeclarativeAgentFactory.GetAgents();

        foreach (MicrosoftFoundryAgent a in agents)
        {
            IList<ProjectsAgentVersion> versions = factory.DeclarativeAgentFactory.GetAgentVersions(a.Name!);
            Console.WriteLine("");
        }

        IList<ProjectsAgentVersion> agentVersions = factory.DeclarativeAgentFactory.GetAgentVersions("myTest");
        */
        //MicrosoftFoundryAgent agent = factory.DeclarativeAgentFactory.GetAgent("myTest", "1");

        /*
        MicrosoftFoundryAgent agent3 = factory.CreateAgent(new AgentOptions
        {
            Model = "gpt-5.6-luna",RawHttpCallDetails = details =>
            {

                Console.WriteLine(details.RequestData);
                Console.WriteLine(details.ResponseData);
            }
        });*/
        /*
        AIProjectClient client = connection.GetClient();

        ChatClientAgent agent = client.GetProjectOpenAIClient().GetProjectResponsesClient().AsAIAgent(model: "gpt-5.6-luna");
        */



        AgentSession session = await agent2.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine() ?? "";
            AgentResponse response2 = await agent2.RunAsync(input, session);
            Console.WriteLine(response2);

            Console.WriteLine("------------------");
        }
    }
}