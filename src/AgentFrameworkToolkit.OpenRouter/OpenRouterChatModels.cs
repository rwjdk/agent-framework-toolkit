using JetBrains.Annotations;

namespace AgentFrameworkToolkit.OpenRouter;

/// <summary>
/// A List of the most common OpenRouter Models
/// </summary>
[PublicAPI]
public static class OpenRouterChatModels
{
    /// <summary>
    /// XAI Models
    /// </summary>
    [PublicAPI]
    public static class XAI
    {
        /// <summary>
        /// Grok 4.7
        /// </summary>
        public const string Grok47 = "x-ai/grok-4.7";

        /// <summary>
        /// Grok 4.6
        /// </summary>
        public const string Grok46 = "x-ai/grok-4.6";

        /// <summary>
        /// Grok 4.5 
        /// </summary>
        public const string Grok45 = "x-ai/grok-4.5";

        /// <summary>
        /// Grok 4.1 Fast (Reasoning)
        /// </summary>
        public const string Grok41FastReasoning = "x-ai/grok-4-1-fast-reasoning";

        /// <summary>
        /// Grok 4.1 Fast (Non-Reasoning)
        /// </summary>
        public const string Grok41FastNonReasoning = "x-ai/grok-4-1-fast-non-reasoning";

        /// <summary>
        /// Grok Code Fast 1
        /// </summary>
        public const string GrokCodeFast1 = "x-ai/grok-code-fast-1";

        /// <summary>
        /// Grok 4 Fast (Reasoning)
        /// </summary>
        public const string Grok4FastReasoning = "x-ai/grok-4-fast-reasoning";

        /// <summary>
        /// Grok 4 Fast (Non-Reasoning)
        /// </summary>
        public const string Grok4FastNonReasoning = "x-ai/grok-4-fast-non-reasoning";

        /// <summary>
        /// Grok 4
        /// </summary>
        public const string Grok4 = "x-ai/grok-4";

        /// <summary>
        /// Grok 3 Mini
        /// </summary>
        public const string Grok3Mini = "x-ai/grok-3-mini";

        /// <summary>
        /// Grok 3
        /// </summary>
        public const string Grok3 = "x-ai/grok-3";

        /// <summary>
        /// Grok 2 Vision
        /// </summary>
        public const string Grok2Vision = "x-ai/grok-2-vision";
    }

    /// <summary>
    /// Anthropic Models
    /// </summary>
    [PublicAPI]
    public static class Anthropic
    {
        /// <summary>
        /// Claude Opus 5.5
        /// </summary>
        public const string ClaudeOpus55 = "anthropic/claude-opus-5.5";

        /// <summary>
        /// Claude Opus 5
        /// </summary>
        public const string ClaudeOpus5 = "anthropic/claude-opus-5";

        /// <summary>
        /// Claude Fable 5.1
        /// </summary>
        public const string ClaudeFable51 = "anthropic/claude-fable-5.1";

        /// <summary>
        /// Claude Sonnet 3.5
        /// </summary>
        public const string ClaudeSonnet35 = "anthropic/claude-3-5-sonnet";

        /// <summary>
        /// Claude Sonnet 3.7
        /// </summary>
        public const string ClaudeSonnet37 = "anthropic/claude-3-7-sonnet";

        /// <summary>
        /// Claude Sonnet 4
        /// </summary>
        public const string ClaudeSonnet4 = "anthropic/claude-sonnet-4";

        /// <summary>
        /// Claude Sonnet 4.5
        /// </summary>
        public const string ClaudeSonnet45 = "anthropic/claude-sonnet-4-5";

        /// <summary>
        /// Claude Sonnet 4.6
        /// </summary>
        public const string ClaudeSonnet46 = "anthropic/claude-sonnet-4-6";

        /// <summary>
        /// Claude Sonnet 5
        /// </summary>
        public const string ClaudeSonnet5 = "anthropic/claude-sonnet-5";

        /// <summary>
        /// Claude Sonnet 5.5
        /// </summary>
        public const string ClaudeSonnet55 = "anthropic/claude-sonnet-5.5";


        /// <summary>
        /// Claude Opus 4
        /// </summary>
        public const string ClaudeOpus4 = "anthropic/claude-opus-4";

        /// <summary>
        /// Claude Opus 4.1
        /// </summary>
        public const string ClaudeOpus41 = "anthropic/claude-opus-4-1";

        /// <summary>
        /// Claude Opus 4.5
        /// </summary>
        public const string ClaudeOpus45 = "anthropic/claude-opus-4-5";

        /// <summary>
        /// Claude Opus 4.6
        /// </summary>
        public const string ClaudeOpus46 = "anthropic/claude-opus-4-6";

        /// <summary>
        /// Claude Opus 4.7
        /// </summary>
        public const string ClaudeOpus47 = "anthropic/claude-opus-4-7";

        /// <summary>
        /// Claude Opus 4.8
        /// </summary>
        public const string ClaudeOpus48 = "anthropic/claude-opus-4-8";

        /// <summary>
        /// Claude Fable 5
        /// </summary>
        public const string ClaudeFable5 = "anthropic/claude-fable-5";

        /// <summary>
        /// Claude Haiku 3.5
        /// </summary>
        public const string ClaudeHaiku35 = "anthropic/claude-3-5-haiku";

        /// <summary>
        /// Claude Haiku 4.5
        /// </summary>
        public const string ClaudeHaiku45 = "anthropic/claude-haiku-4-5";

        /// <summary>
        /// Claude Haiku 3
        /// </summary>
        public const string ClaudeHaiku3 = "anthropic/claude-3-haiku";
    }

    /// <summary>
    /// OpenAI Models
    /// </summary>
    [PublicAPI]
    public static class OpenAI
    {
        /// <summary>
        /// GPT-6.1 Sol (Reasoning)
        /// </summary>
        public const string Gpt61Sol = "openai/gpt-6.1-sol";

        /// <summary>
        /// GPT-6 Astra (Reasoning)
        /// </summary>
        public const string Gpt6Astra = "openai/gpt-6-astra";

        /// <summary>
        /// GPT-6 Sol (Reasoning)
        /// </summary>
        public const string Gpt6Sol = "openai/gpt-6-sol";

        /// <summary>
        /// GPT-6 Luna (Reasoning)
        /// </summary>
        public const string Gpt6Luna = "openai/gpt-6-luna";

        /// <summary>
        /// GPT-5.5 Pro (Reasoning)
        /// </summary>
        public const string Gpt55Pro = "openai/gpt-5.5-pro";

        /// <summary>
        /// GPT-5.6 Sol (Reasoning)
        /// </summary>
        public const string Gpt56Sol = "openai/gpt-5.6-sol";

        /// <summary>
        /// GPT-5.6 Terra (Reasoning)
        /// </summary>
        public const string Gpt56Terra = "openai/gpt-5.6-terra";

        /// <summary>
        /// GPT-5.6 Luna (Reasoning)
        /// </summary>
        public const string Gpt56Luna = "openai/gpt-5.6-luna";

        /// <summary>
        /// GPT-5.5 (Reasoning)
        /// </summary>
        public const string Gpt55 = "openai/gpt-5.5";

        /// <summary>
        /// GPT-5.4 Mini (Reasoning)
        /// </summary>
        public const string Gpt54Mini = "openai/gpt-5.4-mini";

        /// <summary>
        /// GPT-5.4 Nano (Reasoning)
        /// </summary>
        public const string Gpt54Nano = "openai/gpt-5.4-nano";

        /// <summary>
        /// GPT-5.4 Pro (Reasoning)
        /// </summary>
        public const string Gpt54Pro = "openai/gpt-5.4-pro";

        /// <summary>
        /// GPT-5.4 (Reasoning)
        /// </summary>
        public const string Gpt54 = "openai/gpt-5.4";

        /// <summary>
        /// GPT-5.2 Pro (Reasoning)
        /// </summary>
        public const string Gpt52Pro = "openai/gpt-5.2-pro";

        /// <summary>
        /// GPT-5.2 (Reasoning)
        /// </summary>
        public const string Gpt52 = "openai/gpt-5.2";

        /// <summary>
        /// GPT-5.1 (Reasoning)
        /// </summary>
        public const string Gpt51 = "openai/gpt-5.1";

        /// <summary>
        /// GPT-5.3 Codex (Reasoning) [NB: Only work with Responses API]
        /// </summary>
        public const string Gpt53Codex = "openai/gpt-5.3-codex";

        /// <summary>
        /// GPT-5.2 Codex (Reasoning) [NB: Only work with Responses API]
        /// </summary>
        public const string Gpt52Codex = "openai/gpt-5.2-codex";

        /// <summary>
        /// GPT-5.1 Codex (Reasoning) [NB: Only work with Responses API]
        /// </summary>
        public const string Gpt51Codex = "openai/gpt-5.1-codex";

        /// <summary>
        /// GPT-5 Codex (Reasoning) [NB: Only work with Responses API]
        /// </summary>
        public const string Gpt5Codex = "openai/gpt-5-codex";

        /// <summary>
        /// GPT-5 pro (Reasoning)
        /// </summary>
        public const string Gpt5Pro = "openai/gpt-5-pro";

        /// <summary>
        /// GPT-5 (Reasoning)
        /// </summary>
        public const string Gpt5 = "openai/gpt-5";

        /// <summary>
        /// GPT-5-Mini (Reasoning)
        /// </summary>
        public const string Gpt5Mini = "openai/gpt-5-mini";

        /// <summary>
        /// GPT-5-Nano (Reasoning)
        /// </summary>
        public const string Gpt5Nano = "openai/gpt-5-nano";

        /// <summary>
        /// GPT-4.1 (Non-Reasoning)
        /// </summary>
        public const string Gpt41 = "openai/gpt-4.1";

        /// <summary>
        /// GPT-4.1 Mini (Non-Reasoning)
        /// </summary>
        public const string Gpt41Mini = "openai/gpt-4.1-mini";

        /// <summary>
        /// GPT-4.1 Nano (Non-Reasoning)
        /// </summary>
        public const string Gpt41Nano = "openai/gpt-4.1-nano";
    }

    /// <summary>
    /// Google
    /// </summary>
    [PublicAPI]
    public static class Google
    {
        /// <summary>
        /// Gemini 3.8 Flash
        /// </summary>
        public const string Gemini38Flash = "google/gemini-3.8-flash";

        /// <summary>
        /// Gemini 3.7 Flash
        /// </summary>
        public const string Gemini37Flash = "google/gemini-3.7-flash";

        /// <summary>
        /// Gemini 3.6 Flash
        /// </summary>
        public const string Gemini36Flash = "google/gemini-3.6-flash";

        /// <summary>
        /// Gemini 3.5 Flash-Lite
        /// </summary>
        public const string Gemini35FlashLite = "google/gemini-3.5-flash-lite";

        /// <summary>
        /// Gemini 3.5 Flash
        /// </summary>
        public const string Gemini35Flash = "google/gemini-3.5-flash";

        /// <summary>
        /// Gemini 3.1 Flash-Lite
        /// </summary>
        public const string Gemini31FlashLite = "google/gemini-3.1-flash-lite";

        /// <summary>
        /// Gemini 2.5 Pro
        /// </summary>
        public const string Gemini25Pro = "google/gemini-2.5-pro";

        /// <summary>
        /// Gemini 2.5 Flash
        /// </summary>
        public const string Gemini25Flash = "google/gemini-2.5-flash";

        /// <summary>
        /// Gemini 2.5 Flash-Lite
        /// </summary>
        public const string Gemini25FlashLite = "google/gemini-2.5-flash-lite";
    }

    /// <summary>
    /// Mistral Models
    /// </summary>
    [PublicAPI]
    public static class Mistral
    {
        /// <summary>
        /// Mistral Medium 3.5
        /// </summary>
        public const string MistralMedium35 = "mistralai/mistral-medium-3-5";

        /// <summary>
        /// Mistral Small 4
        /// </summary>
        public const string MistralSmall4 = "mistralai/mistral-small-2603";

        /// <summary>
        /// Ministral 3 14B
        /// </summary>
        public const string Ministral314B = "mistralai/ministral-14b-2512";

        /// <summary>
        /// Ministral 3 8B
        /// </summary>
        public const string Ministral38B = "mistralai/ministral-8b-2512";

        /// <summary>
        /// Ministral 3 3B
        /// </summary>
        public const string Ministral33B = "mistralai/ministral-3b-2512";

        /// <summary>
        /// Mistral (Small)
        /// </summary>
        public const string MistralSmall = "mistralai/mistral-small-latest";

        /// <summary>
        /// Mistral (Medium)
        /// </summary>
        public const string MistralMedium = "mistralai/mistral-medium-latest";

        /// <summary>
        /// Mistral (Large)
        /// </summary>
        public const string MistralLarge = "mistralai/mistral-large-latest";
    }
}
