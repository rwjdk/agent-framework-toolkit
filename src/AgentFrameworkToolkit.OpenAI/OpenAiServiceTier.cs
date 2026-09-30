namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// The different OpenAI Service Tiers
/// </summary>
public enum OpenAIServiceTier
{
    /// <summary>
    /// Automatic
    /// </summary>
    Auto,

    /// <summary>
    /// Flex Tier (Cheaper but higher latency) [Note: Not all models support this tier]
    /// </summary>
    Flex,

    /// <summary>
    /// Default Tier
    /// </summary>
    Default,

    /// <summary>
    /// Priority Tier (More Expensive but lower latency)
    /// </summary>
    Priority,

    /// <summary>
    /// Ultrafast Tier (Much more Expensive but much faster) [Note: Not all models support this tier]
    /// </summary>
    Ultrafast,


}