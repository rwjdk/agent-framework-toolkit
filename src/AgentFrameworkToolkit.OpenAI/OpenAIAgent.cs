using JetBrains.Annotations;
using Microsoft.Agents.AI;
using OpenAI;

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// An Agent targeting OpenAI
/// </summary>
[PublicAPI]
public class OpenAIAgent : Agent
{
    private readonly OpenAIDecisionFactory? _decisions;

    /// <summary>Wraps an existing agent without an associated OpenAI SDK client.</summary>
    /// <param name="innerAgent">The inner generic agent.</param>
    public OpenAIAgent(AIAgent innerAgent) : base(innerAgent)
    {
    }

    /// <summary>Wraps an agent and shares its SDK client with Decisions.</summary>
    /// <param name="innerAgent">The inner generic agent.</param>
    /// <param name="client">The existing SDK client used by the agent.</param>
    /// <param name="decisionApiModel">Optional decision model. When omitted, Decisions is unavailable.</param>
    public OpenAIAgent(AIAgent innerAgent, OpenAIClient client, string? decisionApiModel = null) : base(innerAgent)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (decisionApiModel is not null)
        {
            _decisions = new(client, decisionApiModel);
        }
    }

    /// <summary>Gets decision operations sharing this agent's SDK client and transport.</summary>
    /// <exception cref="InvalidOperationException">No decision model was configured.</exception>
    public OpenAIDecisionFactory Decisions => _decisions ?? throw new InvalidOperationException(
        "Decisions is not configured. Set AgentOptions.DecisionApiModel when creating the agent, or supply a decision model and SDK client when wrapping an agent.");
}
