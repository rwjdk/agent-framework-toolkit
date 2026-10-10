# Agent Framework Toolkit @ OpenAI

> This package is aimed at OpenAI as an LLM Provider. Check out the [General README.md](https://github.com/rwjdk/agent-framework-toolkit/blob/main/README.md) for other providers and shared features in Agent Framework Toolkit.

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

1. Install the 'AgentFrameworkToolkit.OpenAI' NuGet Package (`dotnet add package AgentFrameworkToolkit.OpenAI`)
2. Get your [OpenAI API Key](https://platform.openai.com/settings/organization/api-keys)
3. Create an `OpenAIAgentFactory` instance (Namespace: AgentFrameworkToolkit.OpenAI)
4. Use instance to create your `OpenAIAgent` (which is a regular Microsoft Agent Framework `AIAgent` behind the scenes)

### Minimal Code Example
```cs
//Create your AgentFactory
OpenAIAgentFactory agentFactory = new OpenAIAgentFactory("<API Key>");

//Create your Agent
OpenAIAgent agent = agentFactory.CreateAgent("gpt-5");
AgentResponse response = await agent.RunAsync("Hello World");
Console.WriteLine(response);
```

### Normal Code Example
```cs
//Create your AgentFactory
OpenAIAgentFactory agentFactory = new OpenAIAgentFactory("<API Key>");

//Create your Agent
OpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions //Use AgentOptions overload to access more options
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
OpenAIAgentFactory agentFactory = new OpenAIAgentFactory(new OpenAIConnection
{
    //Endpoint = "<endpoint>", //Optional: if targeting non-OpenAI provider
    ApiKey = "<apiKey>",
    NetworkTimeout = TimeSpan.FromMinutes(5), //Set call timeout
    DefaultClientType = ClientType.ResponsesApi, //Set default Client Type for each agent (ChatClient or ResponsesAPI)
    AdditionalOpenAIClientOptions = options =>
    {
        //Set additional properties if needed
    }
});

//Create your Agent
OpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
{
    //Mandatory
    Model = "gpt-5", //Model to use
            
    //Optional (Common)
    ClientType = ClientType.ChatClient, //Choose ClientType (ChatClient or Responses API)
    Name = "MyAgent", //Agent Name
    Temperature = 0, //The Temperature of the LLM Call (1 = Normal; 0 = Less creativity) [ONLY NON-REASONING MODELS]
    ReasoningEffort = OpenAIReasoningEffort.Low, //Set Reasoning Effort [ONLY REASONING MODELS]
    ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed, //Only used in Responses API [ONLY REASONING MODELS]
    StoredOutputEnabled = false, //Set OpenAI's "store" setting
    ServiceTier = OpenAIServiceTier.Default, //Set OpenAI service tier
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
OpenAIBatchRunner batchRunner = new("<apiKey>");

ChatBatchRun run = await batchRunner.RunChatBatchAsync(
    new ChatBatchOptions
    {
        Model = "gpt-5-mini"
    },
    [
        ChatBatchRequest.Create("Summarize this text.")
    ]);

EmbeddingBatchRun embeddingRun = await batchRunner.RunEmbeddingBatchAsync(
    new EmbeddingBatchOptions
    {
        Model = "text-embedding-3-small"
    },
    [
        EmbeddingBatchRequest.Create("Embed this text.")
    ]);
```

> Note: Batch runner APIs are marked experimental with `AFT999`.

## Decisions API

Shared requests, question attributes, and result types live in `AgentFrameworkToolkit.Decisions`.
`OpenAIDecisionFactory` and `JevDecisionFactory` implement `IDecisionFactory` with the same operations.
`AddOpenAIDecisionFactory` registers both the concrete factory and `IDecisionFactory` as the same singleton.
When registering multiple decision providers, resolve their concrete factories; the last registration supplies `IDecisionFactory`.

`OpenAIDecisionFactory` evaluates shared text and inline images using the
[OpenAI Decisions API](https://developers.openai.com/api/docs/guides/decisions).
Use single-question methods or define several questions on a result class.

For a single question, use the convenience methods without defining a result class:

```csharp
Department department = await factory.ChooseAsync<Department>(new ChoiceRequest
{
    Input = text,
    Question = "Which department should handle this?"
});
bool damaged = await factory.IsTrueAsync(new ProbabilityRequest
{
    Input = text,
    Question = "Is the product damaged?",
    Threshold = 0.7
});
Probability probability = await factory.ProbabilityAsync(new ProbabilityImageRequest
{
    Images = [image],
    Input = "Customer photo", // Optional for image evidence.
    Question = "Is the product damaged?",
    ImageDetail = ImageDetail.High
});
Score<Severity> severity = await factory.ScoreAsync<Severity>(new ScoreRequest
{
    Input = text,
    Question = "How severe is the damage?"
});
```

`ChoiceRequest`, `ProbabilityRequest`, and `ScoreRequest` require named `Input` and
`Question` properties. `ChoiceImageRequest`, `ProbabilityImageRequest`, and `ScoreImageRequest` require
`Images` and `Question`, with optional text `Input`. All requests contain
`SafetyIdentifier` directly. Image requests also
expose `ImageDetail`. Probability request `Threshold` defaults to
0.5 and is used by `IsTrueAsync` and the `Probability.IsTrue` returned by
`ProbabilityAsync`. Read `.Value` for the numeric probability. All methods accept
a cancellation token and throw on refused or invalid answers.
`ScoreAsync<TEnum>` returns the weighted score, confidence, level probabilities,
and descriptions; enum numeric order defines zero-based levels. Use
`CreateDecisionAsync<T>` for multiple questions or token usage.

### Runtime-defined questions

Use the non-generic `CreateDecisionAsync` overload when questions and options are
defined at runtime. No result class or enums are required:

```csharp
DynamicDecisionRequest request = new()
{
    Input = "My card was charged twice. Please refund the duplicate.",
    Questions =
    [
        new ChoiceQuestion
        {
            Id = "department",
            Question = "Which team should handle this?",
            Choices =
            [
                new() { Id = "billing", Description = "Charges, payments, and refunds" },
                new() { Id = "technical", Description = "Software defects and outages" }
            ]
        },
        new ScoreQuestion
        {
            Id = "urgency",
            Question = "How urgently should support respond?",
            Levels = ["Routine", "Soon", "Immediately"]
        },
        new ProbabilityQuestion
        {
            Id = "refund_requested",
            Question = "Does the customer request a refund?",
            Threshold = 0.8
        }
    ]
};

DecisionResponse response = await factory.CreateDecisionAsync(request);
ChoiceAnswer department = response.GetChoice("department");
Console.WriteLine(department.Value.Id);
Console.WriteLine(department.Value.Description);
Console.WriteLine(department.Value.Probability);
ScoreAnswer urgency = response.GetScore("URGENCY");
foreach (ScoreLevel level in urgency.Levels)
{
    Console.WriteLine($"{level.Index}: {level.Description} — {level.Probability:P0}");
}
ProbabilityAnswer refund = response.GetProbability("refund_requested");
Console.WriteLine(refund.IsTrue);
Console.WriteLine(response.TotalTokenCount);
```

`Questions` accepts the three built-in `IQuestion` implementations only. IDs and
instructions must be nonempty; question IDs must be unique ignoring case.
Choices require at least two options with nonempty IDs and descriptions, and option
IDs must be unique ignoring case. Scores require at least two nonempty level
descriptions; their order defines zero-based score indices.
`ChoiceAnswer.Options` and `ScoreAnswer.Levels` are immutable snapshots in request
order. `ChoiceAnswer.Value` is the full selected option. `ScoreAnswer.Value` is the
weighted score, with confidence alongside it. `ProbabilityAnswer` exposes `Value`,
`Threshold` (default 0.5), and `IsTrue` using an inclusive threshold.

Answer lookup is case-insensitive. Unknown IDs throw `KeyNotFoundException`, and
using the wrong accessor throws `InvalidOperationException`. Refused, missing, or
invalid answers fail the entire operation, just like typed decisions.

For images, use `DynamicDecisionImageRequest` with required `Images`, required
`Questions`, optional `Input`, and optional `ImageDetail`. Existing inline image
limits and request configuration apply. Dynamic text requests also work with
Azure OpenAI and Jev; those providers continue to reject image requests.

### Attribute-defined questions

For a typed result, define question properties and call `CreateDecisionAsync<T>`:

```csharp
using System.ComponentModel;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Decisions;
using Microsoft.Extensions.AI;

public class SupportDecision
{
    [ProbabilityQuestion("Does the customer report a broken product?", threshold: 0.7)]
    public bool IsBroken { get; set; }

    [ChoiceQuestion<Department>("Which department should handle this?")]
    public Choice<Department>? Department { get; set; }

    [ScoreQuestion<Severity>("How severe is the reported impact?")]
    public Score<Severity>? Severity { get; set; }
}

public enum Department
{
    [Description("Invoices, payments, and subscriptions")]
    Billing,
    [Description("Using or troubleshooting the product")]
    Technical,
    Other
}

public enum Severity
{
    Cosmetic,
    Degraded,
    Blocking
}

// In your async method:
OpenAIDecisionFactory factory = new(apiKey, OpenAIChatModels.Gpt6Luna);
DecisionResponse<SupportDecision> response =
    await factory.CreateDecisionAsync<SupportDecision>(new DecisionRequest
    {
        Input = "The screen arrived broken."
    });
SupportDecision result = response.Result;

// Include inline images; external image URLs and file IDs are unsupported.
DataContent image = new(await File.ReadAllBytesAsync("product.png"), "image/png");
DecisionResponse<SupportDecision> imageResponse =
    await factory.CreateDecisionAsync<SupportDecision>(
        new DecisionImageRequest { Images = [image], ImageDetail = ImageDetail.High });
```

Predicate questions accept `bool`, `double`, `decimal`, their nullable forms,
or `Probability`. `Probability.Value` contains the numeric probability;
`IsTrue` checks the configured `Threshold` (from the request or predicate attribute,
defaulting to 0.5), and `IsAtLeast(threshold)` checks
a custom threshold. Boolean thresholds are inclusive and default to 0.5.
Choice properties accept the attribute's enum, its nullable form, or
`Choice<TEnum>`. Score properties accept `double`, `decimal`, their nullable
forms, or `Score<TEnum>`. Detailed choice and score results include confidence
and a probability for every enum member. Score results also include
`LevelDescriptions` derived from enum descriptions. Read the selected choice or numeric score from
the detailed answer's `Value` property.

Enums must have at least two distinct members without aliases. Score levels follow
ascending enum numeric values and are mapped to indices 0, 1, and so on; the score
is a probability-weighted level index, not an underlying enum value.

The decision model is required when constructing `OpenAIDecisionFactory` and has
no default. Set `SafetyIdentifier` directly on each request. The factory accepts `OpenAIConnection`
for custom endpoints, timeouts, and SDK transport configuration. Register it with
`services.AddOpenAIDecisionFactory(apiKey, decisionModel)` or the connection overload.

Agents created by `OpenAIAgentFactory` expose `agent.Decisions`, sharing the agent's
existing SDK client and HTTP transport:

```csharp
OpenAIAgent agent = new OpenAIAgentFactory(apiKey).CreateAgent(new AgentOptions
{
    Model = agentModel,
    DecisionApiModel = OpenAIChatModels.Gpt6Luna
});
bool damaged = await agent.Decisions.IsTrueAsync(new ProbabilityRequest
{
    Input = "The screen arrived shattered.",
    Question = "Is the product damaged?"
});
```

Decision requests supply their own evidence; agent conversation history and
instructions are not automatically included. The decision model is selected once through `AgentOptions.DecisionApiModel`.
When it is omitted, the agent model is reused only when known to support Decisions
(currently exactly `OpenAIChatModels.Gpt6Luna`). Other models require an explicit
`DecisionApiModel`; otherwise accessing `agent.Decisions` throws a configuration
error. An explicit decision model always overrides the fallback.
Standalone `OpenAIDecisionFactory` instances retain one SDK client, or can accept
an existing `OpenAIClient`. Set `RawHttpCallDetails` on any decision request to
inspect that call's URL and request/response bodies. Attached decisions also inherit
the agent's client-level debugging and transport. Requests reuse the retained client,
including when a request-level debugging callback is set.
An `OpenAIAgent` manually wrapped without an SDK client cannot expose Decisions;
use the constructor accepting the SDK client and decision model instead.

Every attributed property must have a public setter. A refused question throws
`DecisionRefusalException`, exposing the refused `QuestionName`; missing, mismatched,
or invalid answers fail evaluation. Detailed probability distributions and score
descriptions are immutable snapshots. `Probability` validates its value is finite
and between zero and one, including when constructed manually.
No partial result is returned, including when properties are nullable.
Responses include the model and input, output, and total token counts.
At most 128 inline `DataContent` images are allowed per request. Each must contain
nonempty image bytes and an image MIME type. The factory handles Base64 encoding. Set the image request’s `ImageDetail`
to `ImageDetail.Auto` (default), `Low`, `High`, or `Original` for all images in the request.

Text input is optional and can be supplied on an image request:
`factory.CreateDecisionAsync<SupportDecision>(new DecisionImageRequest { Images = [image], Input = "The customer reports screen damage." })`.
