using JetBrains.Annotations;

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// A List of the most common OpenAI Models
/// </summary>
[PublicAPI]
public static class OpenAIChatModels
{
    /// <summary>
    /// GPT-6.1 Sol (Reasoning)
    /// </summary>
    public const string Gpt61Sol = "gpt-6.1-sol";

    /// <summary>
    /// GPT-6 Astra (Reasoning)
    /// </summary>
    public const string Gpt6Astra = "gpt-6-astra";

    /// <summary>
    /// GPT-6 Sol (Reasoning)
    /// </summary>
    public const string Gpt6Sol = "gpt-6-sol";

    /// <summary>
    /// GPT-6 Luna (Reasoning)
    /// </summary>
    public const string Gpt6Luna = "gpt-6-luna";

    /// <summary>
    /// GPT-5.5 Pro (Reasoning)
    /// </summary>
    public const string Gpt55Pro = "gpt-5.5-pro";

    /// <summary>
    /// GPT-5.6 Sol (Reasoning)
    /// </summary>
    public const string Gpt56Sol = "gpt-5.6-sol";

    /// <summary>
    /// GPT-5.6 Terra (Reasoning)
    /// </summary>
    public const string Gpt56Terra = "gpt-5.6-terra";

    /// <summary>
    /// GPT-5.6 Luna (Reasoning)
    /// </summary>
    public const string Gpt56Luna = "gpt-5.6-luna";

    /// <summary>
    /// GPT-5.5 (Reasoning)
    /// </summary>
    public const string Gpt55 = "gpt-5.5";

    /// <summary>
    /// GPT-5.4 Mini (Reasoning)
    /// </summary>
    public const string Gpt54Mini = "gpt-5.4-mini";

    /// <summary>
    /// GPT-5.4 Nano (Reasoning)
    /// </summary>
    public const string Gpt54Nano = "gpt-5.4-nano";

    /// <summary>
    /// GPT-5.4 Pro (Reasoning)
    /// </summary>
    public const string Gpt54Pro = "gpt-5.4-pro";

    /// <summary>
    /// GPT-5.4 (Reasoning)
    /// </summary>
    public const string Gpt54 = "gpt-5.4";

    /// <summary>
    /// GPT-5.2 Pro (Reasoning)
    /// </summary>
    public const string Gpt52Pro = "gpt-5.2-pro";

    /// <summary>
    /// GPT-5.2 (Reasoning)
    /// </summary>
    public const string Gpt52 = "gpt-5.2";

    /// <summary>
    /// GPT-5.1 (Reasoning)
    /// </summary>
    public const string Gpt51 = "gpt-5.1";

    /// <summary>
    /// GPT-5.3 Codex (Reasoning) [NB: Only work with Responses API]
    /// </summary>
    public const string Gpt53Codex = "gpt-5.3-codex";

    /// <summary>
    /// GPT-5.2 Codex (Reasoning) [NB: Only work with Responses API]
    /// </summary>
    public const string Gpt52Codex = "gpt-5.2-codex";

    /// <summary>
    /// GPT-5.1 Codex (Reasoning) [NB: Only work with Responses API]
    /// </summary>
    public const string Gpt51CodexMax = "gpt-5.1-codex";

    /// <summary>
    /// GPT-5.1 Codex (Reasoning) [NB: Only work with Responses API]
    /// </summary>
    public const string Gpt51Codex = "gpt-5.1-codex";

    /// <summary>
    /// GPT-5 Codex (Reasoning) [NB: Only work with Responses API]
    /// </summary>
    public const string Gpt5Codex = "gpt-5-codex";

    /// <summary>
    /// GPT-5 Pro (Reasoning)
    /// </summary>
    public const string Gpt5Pro = "gpt-5-pro";

    /// <summary>
    /// GPT-5 (Reasoning)
    /// </summary>
    public const string Gpt5 = "gpt-5";

    /// <summary>
    /// GPT-5-Mini (Reasoning)
    /// </summary>
    public const string Gpt5Mini = "gpt-5-mini";

    /// <summary>
    /// GPT-5-Nano (Reasoning)
    /// </summary>
    public const string Gpt5Nano = "gpt-5-nano";

    /// <summary>
    /// GPT-4.1 (Non-Reasoning)
    /// </summary>
    public const string Gpt41 = "gpt-4.1";

    /// <summary>
    /// GPT-4.1 Mini (Non-Reasoning)
    /// </summary>
    public const string Gpt41Mini = "gpt-4.1-mini";

    /// <summary>
    /// GPT-4.1 Nano (Non-Reasoning)
    /// </summary>
    public const string Gpt41Nano = "gpt-4.1-nano";

    /// <summary>
    /// GPT-4o (Non-Reasoning)
    /// </summary>
    public const string Gpt4O = "gpt-4o";

    /// <summary>
    /// GPT-4o Mini (Non-Reasoning)
    /// </summary>
    public const string Gpt4OMini = "gpt-4o-mini";

    /// <summary>
    /// Known Non-Reasoning Models
    /// </summary>
    public static string[] NonReasoningModels = [Gpt41, Gpt41Mini, Gpt41Nano, Gpt4O, Gpt4OMini];

    /// <summary>
    /// Known Reasoning Models
    /// </summary>
    public static string[] ReasoningModels = [Gpt5, Gpt5Pro, Gpt51, Gpt5Mini, Gpt5Nano, Gpt5Codex, Gpt51Codex, Gpt51CodexMax, Gpt52Pro, Gpt52, Gpt52Codex, Gpt53Codex, Gpt54, Gpt54Pro, Gpt54Mini, Gpt54Nano, Gpt55, Gpt55Pro, Gpt56Sol, Gpt56Terra, Gpt56Luna, Gpt6Astra, Gpt6Sol, Gpt6Luna, Gpt61Sol];
}
