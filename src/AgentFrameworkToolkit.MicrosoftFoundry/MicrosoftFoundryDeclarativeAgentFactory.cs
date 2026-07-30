using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.ClientModel;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Factory for working with Hosted Agents
/// </summary>
[PublicAPI]
public class MicrosoftFoundryDeclarativeAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public MicrosoftFoundryConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public MicrosoftFoundryDeclarativeAgentFactory(MicrosoftFoundryConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Endpoint of Microsoft Foundry Project</param>
    /// <param name="authenticationTokenProvider">Optional TokenProvider used for credentials; if not provided DefaultAzureCredential will be used</param>
    public MicrosoftFoundryDeclarativeAgentFactory(string endpoint, AuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        Connection = new MicrosoftFoundryConnection(endpoint, authenticationTokenProvider);
    }
    
    /// <summary>
    /// Create an Agent (or update to a new version if agentName exist and there are changes in definition)
    /// </summary>
    /// <param name="agentName">Unique name of the Agent</param>
    /// <param name="model">Model the Agent should use</param>
    /// <param name="instructions">Instructions for the Agent</param>
    /// <param name="tools">Local Tools that the agent should be allowed to call</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent CreateAgent(string agentName, string model, string? instructions = null, IList<AITool>? tools = null)
    {
        DeclarativeAgentDefinition definition = new(model)
        {
            Instructions = instructions,
        };
        foreach (AITool tool in tools ?? [])
        {
            definition.Tools.Add(tool.AsOpenAIResponseTool());
        }
        return CreateAgent(agentName, definition, tools);
    }

    /// <summary>
    /// Create an Agent (or update to a new version if agentName exist and there are changes in definition)
    /// </summary>
    /// <param name="options">Options for the Agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent CreateAgent(DeclarativeAgentCreationOptions options)
    {
        DeclarativeAgentDefinition definition = new(options.Model)
        {
            Instructions = options.Instructions,
        };

        //Tools
        foreach (AITool tool in options.Tools ?? [])
        {
            definition.Tools.Add(tool.AsOpenAIResponseTool());
        }
        foreach (McpTool mcpTool in options.McpTools ?? [])
        {
            definition.Tools.Add(mcpTool);
        }
        if (options.WebSearchTool)
        {
            definition.Tools.Add(new WebSearchTool());
        }
        if (options.CodeInterpreterTool)
        {
            definition.Tools.Add(new CodeInterpreterTool(new CodeInterpreterToolContainer(new AutomaticCodeInterpreterToolContainerConfiguration())));
        }

        //Reasoning
        if (options.ReasoningEffort.HasValue || options.ReasoningSummaryVerbosity.HasValue)
        {
            definition.ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = options.ReasoningEffort,
                ReasoningSummaryVerbosity = options.ReasoningSummaryVerbosity
            };
        }

        AIProjectClient client = Connection.GetClient();
        ProjectsAgentVersionCreationOptions creationOptions = new(definition);
        ClientResult<ProjectsAgentVersion> result = client.AgentAdministrationClient.CreateAgentVersion(options.Name, creationOptions);
        FoundryAgent agent = client.AsAIAgent(result.Value, options.Tools, services: options.Services);
        return new MicrosoftFoundryAgent(MiddlewareHelper.ApplyMiddleware(agent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }

    /// <summary>
    /// Create an Agent (or update to a new version if agentName exist and there are changes in definition)
    /// </summary>
    /// <param name="agentName">Unique Name of Agent</param>
    /// <param name="definition">The raw Microsoft Foundry Agent Definition for advanced scenarios</param>
    /// <param name="tools">In-process Tools used by the Agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent CreateAgent(string agentName, ProjectsAgentDefinition definition, IList<AITool>? tools = null)
    {
        AIProjectClient client = Connection.GetClient();

        ProjectsAgentVersionCreationOptions creationOptions = new(definition);
        ClientResult<ProjectsAgentVersion> result = client.AgentAdministrationClient.CreateAgentVersion(agentName, creationOptions);
        FoundryAgent agent = client.AsAIAgent(result.Value, tools);
        return new MicrosoftFoundryAgent(agent);
    }

    /// <summary>
    /// Get an already defined Agent by its name
    /// </summary>
    /// <param name="agentName">The unique name of the Agent</param>
    /// <param name="tools">In-process Tools used by the Agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent GetAgent(string agentName, IList<AITool>? tools = null)
    {
        AIProjectClient client = Connection.GetClient();
        ClientResult<ProjectsAgentRecord> result = client.AgentAdministrationClient.GetAgent(agentName);
        FoundryAgent agent = client.AsAIAgent(result.Value, tools);
        return new MicrosoftFoundryAgent(agent);
    }

    /// <summary>
    /// Get an already defined Agent by its name and version
    /// </summary>
    /// <param name="agentName">The unique name of the agent</param>
    /// <param name="version">the version of the agent to use ('1,', '2', ...)</param>
    /// <param name="tools">In-process Tools used by the Agent</param>
    /// <returns>The Agent</returns>
    public MicrosoftFoundryAgent GetAgent(string agentName, string version, IList<AITool>? tools = null)
    {
        AIProjectClient client = Connection.GetClient();
        ClientResult<ProjectsAgentVersion> result = client.AgentAdministrationClient.GetAgentVersion(agentName, version);
        FoundryAgent agent = client.AsAIAgent(result.Value, tools);
        return new MicrosoftFoundryAgent(agent);
    }

    /// <summary>
    /// Get all Agents in the Microsoft Foundry Project
    /// </summary>
    /// <param name="tools">In-process Tools used by the Agents</param>
    /// <returns>The Agents</returns>
    public IList<MicrosoftFoundryAgent> GetAgents(IList<AITool>? tools = null)
    {
        const int pageSize = 100;
        List<MicrosoftFoundryAgent> agents = [];
        AIProjectClient client = Connection.GetClient();
        string? after = null;
        List<ProjectsAgentRecord> records;
        do
        {
            CollectionResult<ProjectsAgentRecord> result = client.AgentAdministrationClient.GetAgents(
                kind: ProjectsAgentKind.Prompt,
                limit: pageSize,
                order: AgentListOrder.Ascending,
                after: after);
            records = result.Take(pageSize).ToList();
            foreach (ProjectsAgentRecord record in records)
            {
                FoundryAgent agent = client.AsAIAgent(record, tools);
                agents.Add(new MicrosoftFoundryAgent(agent));
            }
            after = records.LastOrDefault()?.Id;
        }
        while (records.Count == pageSize);
        return agents;
    }

    /// <summary>
    /// Get All Versions of an Agent
    /// </summary>
    /// <param name="agentName">The Unique name of the Agent</param>
    /// <returns>List of AgentVersions for the Agent</returns>
    public IList<ProjectsAgentVersion> GetAgentVersions(string agentName)
    {
        const int pageSize = 100;
        AIProjectClient client = Connection.GetClient();
        List<ProjectsAgentVersion> versions = [];
        string? after = null;
        List<ProjectsAgentVersion> page;
        do
        {
            CollectionResult<ProjectsAgentVersion> result = client.AgentAdministrationClient.GetAgentVersions(
                agentName,
                limit: pageSize,
                after: after);
            page = result.Take(pageSize).ToList();
            versions.AddRange(page);
            after = page.LastOrDefault()?.Id;
        }
        while (page.Count == pageSize);
        return versions;
    }

    /// <summary>
    /// Delete an Agent
    /// </summary>
    /// <param name="agentName">The unique name of the Agent to delete</param>
    public void DeleteAgent(string agentName)
    {
        Connection.GetClient().AgentAdministrationClient.DeleteAgent(agentName);
    }
}
