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

        string agentName = "my-hosted-agent";
        HostedAgentDefinition definition = new(cpu: "0.5", memory: "1Gi")
        {
            CodeConfiguration = new CodeConfiguration(
                runtime: "dotnet_10",
                entryPoint: ["dotnet", "MyHostedAgent.dll"],
                dependencyResolution: CodeDependencyResolution.RemoteBuild),
        };
        definition.Versions.Add(new ProtocolVersionRecord(ProjectsAgentProtocol.Responses, "1.0.0"));
        definition.EnvironmentVariables["AZURE_AI_MODEL_DEPLOYMENT_NAME"] = "gpt-5.6-luna";

        AgentVersionFromCodeMetadata metadata = new(definition);

        AIProjectClient projectClient = new(new Uri("<foundry-project-endpoint>"), new DefaultAzureCredential());
        ClientResult<ProjectsAgentVersion> result = projectClient.AgentAdministrationClient.CreateAgentVersionFromCode(
            agentName: agentName,
            filePath: "<folder-path-to-source-code>", //IMPORTANT: May only contain CSPROJ + CS Files
            metadata);

        ProjectsAgentVersion agentVersion = result.Value;
        while (agentVersion.Status != AgentVersionStatus.Active && agentVersion.Status != AgentVersionStatus.Failed)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            agentVersion = projectClient.AgentAdministrationClient.GetAgentVersion(agentName, agentVersion.Version).Value;
        }
        
        ProjectResponsesClient client = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(agentName);
        ChatClientAgent agent = client.AsAIAgent();
        
        AgentResponse response = await agent.RunAsync("Hello Foundry!");


        MicrosoftFoundryAgent agent55 = factory.HostedAgentFactory.GetAgent("my-hosted-agent4");


        MicrosoftFoundryAgent foundryAgent = factory.HostedAgentFactory.CreateAgent(new HostedAgentCreationOptions
        {
            SourceDirectory = @"C:\Users\rasmu\AppData\Local\Temp\AgentFrameworkToolkit\MyHostedAgent",
            AssemblyName = "MyHostedAgent.dll",
            DotNetRuntime = "dotnet_10",
            Cpu = "0.5",
            Memory = "1Gi",
            Name = "my-hosted-agent2"
        });

        AgentResponse runAsync = await foundryAgent.RunAsync("Hello");


        MicrosoftFoundryAgent agent2 = factory.HostedAgentFactory.GetAgent(
            agentName: "my-hosted-agent4");

        AgentResponse agentResponse = await agent2.RunAsync("What is the capital of France?");

        //MicrosoftFoundryAgent agent2 = factory.DeclarativeAgentFactory.CreateAgent("myTest", "gpt-5.6-luna");
        /*
        MicrosoftFoundryAgent agent3 = factory.DeclarativeAgentFactory.CreateAgent(new DeclarativeAgentCreationOptions
        {
            Name = "MyCoolAgent",
            Model = "gpt-5.6-luna",
            Instructions = "Speak like a pirate",
            ReasoningEffort = ResponseReasoningEffortLevel.Low,
            WebSearchTool = true,
            CodeInterpreterTool = true,
            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
        });
        */
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