using JetBrains.Annotations;

namespace AgentFrameworkToolkit.AzureOpenAI;

/// <summary>Selects the decision API used by an Azure OpenAI decision factory.</summary>
[PublicAPI]
public enum DecisionProtocol
{
    /// <summary>
    /// The Jev protocol hosted as Microsoft Decision-1 at /providers/microsoft/v1/systemone.
    /// Supports text evidence with typed or runtime-defined choice, probability, and score questions.
    /// Image inputs and SafetyIdentifier are not supported.
    /// </summary>
    Jev,

    /// <summary>
    /// The OpenAI Decisions API at /openai/v1/decisions.
    /// Supports text, inline images, and image URLs with typed or runtime-defined choice, probability, and score questions.
    /// Supports SafetyIdentifier and requires a deployment of a model supporting the OpenAI Decisions API.
    /// </summary>
    OpenAI
}
