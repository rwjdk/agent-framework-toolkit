using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Models;

namespace AgentFrameworkToolkit.OpenAI;

internal static class OpenAIModelDiscovery
{
    internal static async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(OpenAIClient client, CancellationToken cancellationToken)
    {
        ClientResult<OpenAIModelCollection> response = await client.GetOpenAIModelClient().GetModelsAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(response.GetRawResponse().Content);
        List<ModelInfo> models = [];
        foreach (JsonElement model in document.RootElement.GetProperty("data").EnumerateArray())
        {
            models.Add(new ModelInfo
            {
                Name = model.GetProperty("id").GetString() ?? throw new JsonException("The provider returned a model without an identifier."),
                RawRepresentation = model.Clone()
            });
        }

        return models;
    }
}
