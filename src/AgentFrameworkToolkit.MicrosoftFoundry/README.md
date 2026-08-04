# Agent Framework Toolkit @ Microsoft Foundry

> This package is aimed at Microsoft Foundry as an LLM Provider. Check out the [General README.md](https://github.com/rwjdk/AgentFrameworkToolkit/blob/main/README.md) for other providers and shared features in Agent Framework Toolkit.

## What is Agent Framework Toolkit?
Agent Framework Toolkit is an opinionated C# wrapper on top of the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) that makes various things easier to work with:
- Easier to set advanced Agent Options ([often only needing half or fewer lines of code to do the same things](https://github.com/rwjdk/AgentFrameworkToolkit/blob/main/README.md)) that normally would need the Breaking Glass approach.
- Easier [Tools / MCP Tools Definition](https://github.com/rwjdk/AgentFrameworkToolkit/blob/main/README.md)
- Easier Structured Output calling with `.RunAsync<>(...)` even on AIAgents using Tool Calling Middleware.

### FAQ

**Q: If I use the Agent Framework Toolkit, does it limit or hinder what I can do with Microsoft Agent Framework?**

A: No, everything you can do with Microsoft Agent Framework can still be done with Agent Framework Toolkit. It is just a wrapper that enables options that are hard to use otherwise

**Q: What is the release frequency of Agent Framework Toolkit (can I always use the latest Microsoft Agent Framework release)?**

A: This NuGet package is released as often (or more) than the Microsoft Agent Framework. At a minimum, it will be bumped to the latest Microsoft Agent Framework Release within a day of official release. It follows the same versioning scheme as AF, so the same or higher version number will always be compatible with the latest release.

**Q: Why are the agents not AIAgent / ChatClientAgents? Are they compatible with the rest of the Microsoft Agent Framework?**

A: The specialized agents in Agent Framework Toolkit are all 100% compatible with AF as they simply inherit from AIAgent


## Getting Started

1. Install the 'AgentFrameworkToolkit.MicrosoftFoundry' NuGet Package (`dotnet add package AgentFrameworkToolkit.MicrosoftFoundry`)
2. Get your [Microsoft Foundry Project Endpoint](https://ai.azure.com)
3. Ensure you have RBAC Access (Example using Azure CLI)
4. Create an `MicrosoftFoundryAgentFactory` instance (Namespace: AgentFrameworkToolkit.MicrosoftFoundry)
5. Use instance to create your `MicrosoftFoundryAgent` (which is a regular Microsoft Agent Framework `AIAgent` behind the scenes)

### Minimal Code Example
```cs
//Create your AgentFactory
MicrosoftFoundryAgentFactory agentFactory = new MicrosoftFoundryAgentFactory("<Endpoint>", new AzureCliCredential());

//Create your Agent
MicrosoftFoundryAgent agent = agentFactory.CreateAgent("gpt-5");
AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```

## Declarative Agents

`MicrosoftFoundryDeclarativeAgentFactory` creates and manages declarative agents whose definitions are stored in your Microsoft Foundry project. This differs from `MicrosoftFoundryAgentFactory`, which creates an agent backed directly by a model client without creating a persistent agent definition in the project.

Creating an agent with a new name creates its first version. Creating it again with the same name and a changed definition creates a new version. Because these agents persist in Microsoft Foundry, delete temporary agents when they are no longer needed.

### Create and manage a declarative agent

```cs
MicrosoftFoundryConnection connection = new()
{
    Endpoint = "<Endpoint>",
    AuthenticationTokenProvider = new AzureCliCredential()
};

MicrosoftFoundryDeclarativeAgentFactory agentFactory = new(connection);
string agentName = $"my-agent-name";
bool agentCreated = false;

try
{
    MicrosoftFoundryAgent agent = agentFactory.CreateAgent(new DeclarativeAgentCreationOptions
    {
        Name = agentName,
        Model = "gpt-5-nano",
        Instructions = "You are a concise assistant.",
        Tools = [],
        McpTools = [],
        WebSearchTool = false,
        CodeInterpreterTool = false
    });
    agentCreated = true;

    AgentResponse response = await agent.RunAsync("Hello World");
    Console.WriteLine(response);

    //Retrieve the latest version, inspect all versions, or select a specific version
    MicrosoftFoundryAgent latestAgent = agentFactory.GetAgent(agentName);
    IList<ProjectsAgentVersion> versions = agentFactory.GetAgentVersions(agentName);
    MicrosoftFoundryAgent firstVersion = agentFactory.GetAgent(agentName, versions[0].Version);

    //List all declarative prompt agents in the project
    IList<MicrosoftFoundryAgent> agents = agentFactory.GetAgents();
}
finally
{
    if (agentCreated)
    {
        //Optional: Deletes the persistent agent and its versions from Microsoft Foundry (if you want to clean-up as part of testing. In production code you let the agent remain)
        agentFactory.DeleteAgent(agentName);
    }
}
```

The factory provides three creation levels:

| Overload | Use case |
| --- | --- |
| `CreateAgent(string agentName, string model, ...)` | Create a simple declarative agent with instructions and local tools. |
| `CreateAgent(DeclarativeAgentCreationOptions options)` | Configure local tools, MCP tools, web search, code interpreter, reasoning, and middleware. |
| `CreateAgent(string agentName, DeclarativeAgentDefinition definition)` | Supply the raw Microsoft Foundry definition for advanced scenarios. |

The returned `MicrosoftFoundryAgent` is a regular Microsoft Agent Framework `AIAgent` and can be run with `RunAsync` or used anywhere an `AIAgent` is accepted.

## Hosted Agents

`MicrosoftFoundryHostedAgentFactory` deploys a .NET agent project to Microsoft Foundry managed hosting. Foundry builds the source remotely and stores the deployment as a hosted agent version. Creating an agent again with the same name deploys a new version.

The hosted factory is available from `MicrosoftFoundryAgentFactory.HostedAgentFactory`, or it can be constructed directly from a `MicrosoftFoundryConnection` or project endpoint.

### Deploy a hosted agent

```cs
MicrosoftFoundryAgentFactory agentFactory = new(
    "<projectEndpoint>",
    new AzureCliCredential());

MicrosoftFoundryHostedAgentFactory hostedFactory = agentFactory.HostedAgentFactory;

MicrosoftFoundryAgent agent = hostedFactory.CreateAgent(new HostedAgentCreationOptions
{
    Name = "my-hosted-agent",
    SourceDirectory = Path.GetFullPath("../MyHostedAgent"),
    AssemblyName = "MyHostedAgent.dll",
    DotNetRuntime = "dotnet_10",
    Cpu = "0.5",
    Memory = "1Gi"
});

AgentResponse response = await agent.RunAsync("Hello Foundry!");
Console.WriteLine(response);
```

`CreateAgent` uploads the source project, waits for the new version to become active, and returns a `MicrosoftFoundryAgent` connected to the deployed endpoint. If the deployment fails, it throws an `InvalidOperationException` containing the final deployment status.

| Option | Description |
| --- | --- |
| `Name` | Unique hosted agent name. Reusing the name deploys a new version. |
| `SourceDirectory` | Directory containing the hosted agent's project and C# source files. |
| `AssemblyName` | DLL produced by the project and used as the hosted process entry point. |
| `DotNetRuntime` | Foundry .NET runtime, for example `dotnet_10`. |
| `Cpu` | Hosted CPU allocation: `"0.5"`, `"1"`, or `"2"`. |
| `Memory` | Hosted memory allocation: `"1Gi"`, `"2Gi"`, or `"4Gi"`. |

The source directory is copied to a temporary staging directory before upload. `launchSettings.json`, `bin`, `obj`, `.git`, and `.vs` content is excluded, and Foundry performs dependency restoration and compilation using its remote build.

### Connect to an existing hosted agent

Use the deployed agent name to connect without uploading or creating a new version:

```cs
MicrosoftFoundryAgentFactory agentFactory = new(
    "<projectEndpoint>",
    new AzureCliCredential());

MicrosoftFoundryAgent agent =
    agentFactory.HostedAgentFactory.GetAgent("my-hosted-agent");

AgentResponse response = await agent.RunAsync("Hello again!");
Console.WriteLine(response);
```

The returned `MicrosoftFoundryAgent` is a regular Microsoft Agent Framework `AIAgent`, so it supports sessions, `RunAsync`, and other Agent Framework features.

### Normal Code Example (RBAC)
```cs
//Create your AgentFactory
MicrosoftFoundryAgentFactory agentFactory = new MicrosoftFoundryAgentFactory(new MicrosoftFoundryConnection
{
    Endpoint = "<Endpoint>",
    AuthenticationTokenProvider = new AzureCliCredential() //Or similar
});

//Create your Agent
MicrosoftFoundryAgent agent = agentFactory.CreateAgent(new AgentOptions //Use AgentOptions overload to access more options
{
    Model = "gpt-5",
    ReasoningEffort = OpenAIReasoningEffort.Low, //Set reasoning effort
    Instructions = "You are a nice AI", //The System Prompt
    Tools = [], //Add your tools here
});

AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```

### Full Code example with ALL options
```cs
//Create your AgentFactory (using a connection object for more options)
MicrosoftFoundryAgentFactory agentFactory = new MicrosoftFoundryAgentFactory(new MicrosoftFoundryConnection
{
    Endpoint = "<endpoint>",
    AuthenticationTokenProvider = new AzureCliCredential(), //Or similar
    NetworkTimeout = TimeSpan.FromMinutes(5), //Set call timeout
    DefaultClientType = ClientType.ResponsesApi //Set default Client Type for each agent (ChatClient or ResponsesAPI)
});

//Create your Agent
MicrosoftFoundryAgent agent = agentFactory.CreateAgent(new AgentOptions
{
    //Mandatory
    Model = "gpt-5", //Model to use
            
    //Optional (Common)
    ClientType = ClientType.ChatClient, //Choose ClientType (ChatClient or Responses API)
    Name = "MyAgent", //Agent Name
    Temperature = 0, //The Temperature of the LLM Call (1 = Normal; 0 = Less creativity) [ONLY NON-REASONING MODELS]
    ReasoningEffort = OpenAIReasoningEffort.Low, //Set Reasoning Effort [ONLY REASONING MODELS]
    ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed, //Only used in Responses API [ONLY REASONING MODELS]
    Instructions = "You are a nice AI", //The System Prompt for the Agent to Follow
    Tools = [], //Add your tools for Tool Calling here
    ToolCallingMiddleware = async (callingAgent, context, next, token) => //Tool Calling Middleware to Inspect, change, and cancel tool-calling
    {
        AIFunctionArguments arguments = context.Arguments; //Details on the tool-call that is about to happen
        return await next(context, token);
    },
    OpenTelemetryMiddleware = new OpenTelemetryMiddleware(source: "MyOpenTelemetrySource", telemetryAgent => telemetryAgent.EnableSensitiveData = true), //Configure OpenTelemetry Middleware

    //Optional (Rarely used)
    MaxOutputTokens = 2000, //Max allow token
    Id = "1234", //Set the ID of Agent (else a random GUID is assigned as ID)
    Description = "My Description", //Description of the Agent (not used by the LLM)
    LoggingMiddleware = new LoggingMiddleware( /* Configure custom logging */),
    Services = null, //Setup Tool Calling Service Injection (See https://youtu.be/EGs-Myf5MB4 for more details)
    LoggerFactory = null, //Setup logger Factory (Alternative to Middleware)
    ChatHistoryProvider = new MyChatMessageStore(), //Set a custom message store
    AIContextProviders = [new MyAIContextProvider()], //Set custom AI context providers
    AdditionalChatClientAgentOptions = options =>
    {
        //Option to set even more options if not covered by AgentFrameworkToolkit
    },
    RawToolCallDetails = Console.WriteLine, //Raw Tool calling Middleware (if you just wish to log what tools are being called. ToolCallingMiddleware is a more advanced version of this)
    RawHttpCallDetails = details => //Intercept the raw HTTP Call to the LLM (great for advanced debugging sessions)
    {
        Console.WriteLine(details.RequestUrl);
        Console.WriteLine(details.RequestData);
        Console.WriteLine(details.ResponseData);
    },
    ClientFactory = client =>
    {
        //Interact with the underlying Client-factory
        return client;
    }
});

AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```
