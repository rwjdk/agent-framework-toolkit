# AgentFrameworkToolkit.Jev

Evaluate text with the Jev Decisions API using the same requests, attributes, results,
and `IDecisionFactory` contract as OpenAI. This package contains its own HTTP implementation,
adapted from JevDotNet, and has no dependency on the JevDotNet package.

## Usage

Supply an API key and model explicitly. There is no default model.

```csharp
using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.Jev;

IDecisionFactory factory = new JevDecisionFactory(apiKey, decisionModel);

bool damaged = await factory.IsTrueAsync(new ProbabilityRequest
{
    Input = "The screen arrived shattered.",
    Question = "Is the product damaged?",
    Threshold = 0.7
});

DecisionResponse<SupportDecision> response =
    await factory.CreateDecisionAsync<SupportDecision>(new DecisionRequest
    {
        Input = "The screen arrived shattered."
    });

public class SupportDecision
{
    [ProbabilityQuestion("Is the product damaged?", threshold: 0.7)]
    public bool IsDamaged { get; set; }

    [ChoiceQuestion<Department>("Which department should handle this?")]
    public Department Department { get; set; }

    [ScoreQuestion<Severity>("How severe is the damage?")]
    public Score<Severity>? Severity { get; set; }
}

public enum Department { Support, Returns }
public enum Severity { Minor, Moderate, Severe }
```

`ChooseAsync<TEnum>` returns an enum choice. `ProbabilityAsync` returns a
`Probability` whose `IsTrue` uses the request threshold. `ScoreAsync<TEnum>`
returns the weighted score, confidence, probabilities, and level descriptions.
For multiple questions or token usage, use `CreateDecisionAsync<T>`.

For runtime-defined questions, use the non-generic `CreateDecisionAsync` overload
with `DynamicDecisionRequest`. Supply a list of `ChoiceQuestion`, `ScoreQuestion`,
and `ProbabilityQuestion` objects, then retrieve answers directly through
`GetChoice(id)`, `GetScore(id)`, and `GetProbability(id)` on the response.
IDs and lookup are case-insensitive; unknown IDs and mismatched accessors throw.
See the [shared usage example](../AgentFrameworkToolkit.OpenAI/README.md#runtime-defined-questions).
The existing text-only restriction and limit of 2–10 score levels also apply to
dynamic requests.

Predicate properties support `bool`, `double`, `decimal`, their nullable forms,
and `Probability`. Numeric probabilities ignore the attribute threshold.
Choice properties support the enum, its nullable form, or `Choice<TEnum>`.
Score properties support `double`, `decimal`, their nullable forms, or
`Score<TEnum>`. Enum numeric order defines zero-based levels, and
`DescriptionAttribute` supplies criteria. Jev score questions require 2–10 levels.
Question properties require public setters. Refused, missing, or invalid answers
throw; no partial result is returned.

Responses include the returned model and input, output, and total token counts.
Total tokens are calculated from Jev's input and output counts.

## Connection and dependency injection

```csharp
JevConnection connection = new(apiKey)
{
    Endpoint = new Uri("https://api.typesafe.ai/v1/systemone"),
    NetworkTimeout = TimeSpan.FromSeconds(60),
    HttpClientFactory = () => httpClient
};

JevDecisionFactory factory = new(connection, decisionModel);
services.AddJevDecisionFactory(connection, decisionModel);
// Or: services.AddJevDecisionFactory(apiKey, decisionModel);
```

The factory retains its connection settings and HTTP client at construction.
The default endpoint is Jev's `/v1/systemone` endpoint. A supplied HTTP client
remains caller-owned. `NetworkTimeout` applies per request without changing
the shared HTTP client's timeout. Requests accept cancellation tokens.
Set `RawHttpCallDetails` on a request to inspect its URL and request/response
bodies, including HTTP error responses.

DI registers `JevDecisionFactory` and `IDecisionFactory` as the same singleton.
When registering multiple decision providers, resolve their concrete factories;
the last registration supplies `IDecisionFactory`.

## Current limitations

All image overloads are present for contract compatibility and throw
`NotSupportedException` until Jev supports images. `SafetyIdentifier` is
unsupported by Jev and throws when supplied.

## Migration from OpenAI-specific contracts

Use `AgentFrameworkToolkit.Decisions` instead of
`AgentFrameworkToolkit.OpenAI.Decisions`, and `DecisionResponse<T>` instead
of `OpenAIDecisionResponse<T>`. Your attributed result classes and requests
can then be used with either decision provider.
