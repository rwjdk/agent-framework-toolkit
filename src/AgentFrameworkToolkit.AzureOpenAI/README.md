# Agent Framework Toolkit @ Azure OpenAI

> This package is aimed at Azure OpenAI as an LLM Provider. Check out the [General README.md](https://github.com/rwjdk/agent-framework-toolkit/blob/main/README.md) for other providers and shared features in Agent Framework Toolkit.

## What is Agent Framework Toolkit?
Agent Framework Toolkit is an opinionated C# wrapper on top of the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) that makes various things easier to work with:
- Easier to set advanced Agent Options ([often only needing half or fewer lines of code to do the same things](https://github.com/rwjdk/agent-framework-toolkit/blob/main/README.md)) that normally would need the Breaking Glass approach.
- Easier [Tools / MCP Tools Definition](https://github.com/rwjdk/agent-framework-toolkit/blob/main/README.md)
- Easier Structured Output calling with `.RunAsync<>(...)` even on AIAgents using Tool Calling Middleware.

### FAQ

**Q: If I use the Agent Framework Toolkit, does it limit or hinder what I can do with Microsoft Agent Framework?**

A: No, everything you can do with Microsoft Agent Framework can still be done with Agent Framework Toolkit. It is just a wrapper that enables options that are hard to use otherwise

**Q: What is the release frequency of Agent Framework Toolkit (can I always use the latest Microsoft Agent Framework release)?**

A: This NuGet package is released as often (or more) than the Microsoft Agent Framework. At a minimum, it will be bumped to the latest Microsoft Agent Framework Release within a day of official release. It follows the same versioning scheme as AF, so the same or higher version number will always be compatible with the latest release.

**Q: Why are the agents not AIAgent / ChatClientAgents? Are they compatible with the rest of the Microsoft Agent Framework?**

A: The specialized agents in Agent Framework Toolkit are all 100% compatible with AF as they simply inherit from AIAgent


## Getting Started

1. Install the 'AgentFrameworkToolkit.AzureOpenAI' NuGet Package (`dotnet add package AgentFrameworkToolkit.AzureOpenAI`)
2. Get your [Azure OpenAI API Key](https://ai.azure.com)
3. Create an `AzureOpenAIAgentFactory` instance (Namespace: AgentFrameworkToolkit.AzureOpenAI)
4. Use instance to create your `AzureOpenAIAgent` (which is a regular Microsoft Agent Framework `AIAgent` behind the scenes)

### Minimal Code Example
```cs
//Create your AgentFactory
AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory("<Endpoint>", "<API Key>");

//Create your Agent
AzureOpenAIAgent agent = agentFactory.CreateAgent("gpt-5");
AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```

### Normal Code Example (API KEY)
```cs
//Create your AgentFactory
AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory("<Endpoint>", "<API Key>");

//Create your Agent
AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions //Use AgentOptions overload to access more options
{
    Model = "gpt-5",
    ReasoningEffort = OpenAIReasoningEffort.Low, //Set reasoning effort
    Instructions = "You are a nice AI", //The System Prompt
    Tools = [], //Add your tools here
});

AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```

### Normal Code Example (RBAC)
```cs
//Create your AgentFactory
AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory(new AzureOpenAIConnection
{
    Endpoint = "<Endpoint>",
    Credentials = new AzureCliCredential() //Or similar
});

//Create your Agent
AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions //Use AgentOptions overload to access more options
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
AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory(new AzureOpenAIConnection
{
    Endpoint = "<endpoint>",
    ApiKey = "<apiKey>",
    NetworkTimeout = TimeSpan.FromMinutes(5), //Set call timeout
    Credentials = null, //Set RBAC Credentials
    DefaultClientType = ClientType.ResponsesApi, //Set default Client Type for each agent (ChatClient or ResponsesAPI)
    AutoCorrectFoundryEndpoint = true, //Autocorrect Foundry project URLs to the Azure OpenAI endpoint
    AdditionalOpenAIClientOptions = options =>
    {
        //Set additional properties if needed
    }
});

//Create your Agent
AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
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

### Batch Runner
```cs
AzureOpenAIBatchRunner batchRunner = new("<endpoint>", "<apiKey>");

ChatBatchRun run = await batchRunner.RunChatBatchAsync(
    new ChatBatchOptions
    {
        Model = "<deployment-name>"
    },
    [
        ChatBatchRequest.Create("Summarize this text.")
    ]);
```

> Note: Batch runner APIs are marked experimental with `AFT999`. In Azure OpenAI, `Model` is your deployment name.
