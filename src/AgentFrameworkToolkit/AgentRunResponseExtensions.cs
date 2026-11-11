using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit;

/// <summary>
/// Various handy Extensions for an AgentResponse
/// </summary>
public static class AgentResponseExtensions
{
    /// <summary>
    /// Retrieve the TextResponseContent from the response (if any)
    /// </summary>
    /// <param name="response">The response to extract the content from</param>
    /// <returns>TextReasoningContent or null if no reasoning text</returns>
    public static TextReasoningContent? GetTextReasoningContent(this AgentResponse response)
    {
        foreach (ChatMessage message in response.Messages)
        {
            foreach (AIContent content in message.Contents)
            {
                if (content is TextReasoningContent textReasoningContent)
                {
                    return textReasoningContent;
                }
            }
        }

        return null;
    }
}
