using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using OpenAI.Responses;
using System.ClientModel;

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Factory for working with Declarative (prompt) Agents
/// </summary>
[PublicAPI]
public class MicrosoftFoundryHostedAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public MicrosoftFoundryConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public MicrosoftFoundryHostedAgentFactory(MicrosoftFoundryConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Endpoint of Microsoft Foundry Project</param>
    /// <param name="authenticationTokenProvider">Optional TokenProvider used for credentials; if not provided DefaultAzureCredential will be used</param>
    public MicrosoftFoundryHostedAgentFactory(string endpoint, AuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        Connection = new MicrosoftFoundryConnection(endpoint, authenticationTokenProvider);
    }

    /// <summary>
    /// Get Agent previously deployed
    /// </summary>
    /// <param name="agentName">Name of Agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent GetAgent(string agentName)
    {
        AIProjectClient projectClient = Connection.GetClient();
        ProjectResponsesClient client = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(agentName);
        ChatClientAgent agent = client.AsAIAgent();

        return new MicrosoftFoundryAgent(agent);
    }
    
    /// <summary>
    /// Create a Hosted Agent from local source code (or update to a new version if agentName exists)
    /// </summary>
    /// <param name="options">Options for the agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent CreateAgent(HostedAgentCreationOptions options)
    {
        HostedAgentDefinition definition = new(options.Cpu, options.Memory)
        {
            CodeConfiguration = new CodeConfiguration(
                runtime: options.DotNetRuntime,
                entryPoint: ["dotnet", options.AssemblyName],
                dependencyResolution: CodeDependencyResolution.RemoteBuild),
        };
        definition.Versions.Add(new ProtocolVersionRecord(ProjectsAgentProtocol.Responses, "1.0.0"));
        definition.EnvironmentVariables["AZURE_AI_MODEL_DEPLOYMENT_NAME"] = "gpt-5.6-luna";

        AgentVersionFromCodeMetadata metadata = new(definition);

        AIProjectClient projectClient = Connection.GetClient();
        string stagingDirectory = Path.Combine(
            Path.GetTempPath(),
            "AgentFrameworkToolkit",
            Guid.NewGuid().ToString("N"));

        CopySourceDirectory(options.SourceDirectory, stagingDirectory);
        ProjectsAgentVersion agentVersion;
        try
        {
            ClientResult<ProjectsAgentVersion> result = projectClient.AgentAdministrationClient.CreateAgentVersionFromCode(
                options.Name,
                stagingDirectory,
                metadata);
            agentVersion = result.Value;
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }

        while (agentVersion.Status != AgentVersionStatus.Active && agentVersion.Status != AgentVersionStatus.Failed)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            agentVersion = projectClient.AgentAdministrationClient.GetAgentVersion(options.Name, agentVersion.Version).Value;
        }

        if (agentVersion.Status != AgentVersionStatus.Active)
        {
            throw new InvalidOperationException($"Agent deployment failed, status: {agentVersion.Status}.");
        }

        ProjectResponsesClient client = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(options.Name);
        ChatClientAgent agent = client.AsAIAgent();
        return new MicrosoftFoundryAgent(agent);
    }

    private static void CopySourceDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string file in Directory.EnumerateFiles(sourceDirectory))
        {
            if (Path.GetFileName(file).Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)));
        }

        foreach (string directory in Directory.EnumerateDirectories(sourceDirectory))
        {
            string directoryName = Path.GetFileName(directory);
            if (directoryName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                directoryName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                directoryName.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                directoryName.Equals(".vs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CopySourceDirectory(directory, Path.Combine(destinationDirectory, directoryName));
        }
    }
}
