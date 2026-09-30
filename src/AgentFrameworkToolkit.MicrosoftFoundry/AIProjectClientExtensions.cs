using AgentFrameworkToolkit.OpenAI;
using Azure.AI.Projects;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Extensions for AIProjectClient
/// </summary>
public static class AIProjectClientExtensions
{
    /// <summary>
    /// Create an Agent
    /// </summary>
    /// <param name="client">ResponsesAPI Client</param>
    /// <param name="options">Options</param>
    /// <returns>Agent</returns>
    public static MicrosoftFoundryAgent AsAIAgent(this AIProjectClient client, AgentOptions options)
    {
        return new MicrosoftFoundryAgent(client.GetProjectOpenAIClient().GetResponsesClient().AsAIAgent(options));
    }
}