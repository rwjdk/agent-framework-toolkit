using System.Diagnostics.CodeAnalysis;
using AgentFrameworkToolkit.OpenAI.Batching;
using Azure.Core;
using System.Text.Json;
using JetBrains.Annotations;
using OpenAI;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.AzureOpenAI.Batching;

/// <summary>
/// Batch Runner for Azure OpenAI
/// </summary>
[PublicAPI]
[Experimental("AFT999")]
public class AzureOpenAIBatchRunner
{
    private readonly InternalBatchRunner _internalBatchRunner;

    /// <summary>
    /// Connection
    /// </summary>
    public AzureOpenAIConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Your AzureOpenAI Endpoint (not to be confused with a Microsoft Foundry Endpoint. format: 'https://YourName.openai.azure.com' or 'https://YourName.services.azure.com')</param>
    /// <param name="apiKey">Your AzureOpenAI API Key (if you need a more advanced connection use the constructor overload)</param>
    public AzureOpenAIBatchRunner(string endpoint, string apiKey)
    {
        Connection = new AzureOpenAIConnection
        {
            Endpoint = endpoint,
            ApiKey = apiKey
        };
        OpenAIClient client = Connection.GetClient();
        _internalBatchRunner = new InternalBatchRunner(client.GetBatchClient(), client.GetOpenAIFileClient(), true);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Your AzureOpenAI Endpoint (not to be confused with a Microsoft Foundry Endpoint. format: 'https://YourName.openai.azure.com' or 'https://YourName.services.azure.com')</param>
    /// <param name="credentials">Your RBAC Credentials (if you need a more advanced connection use the constructor overload)</param>
    public AzureOpenAIBatchRunner(string endpoint, TokenCredential credentials)
    {
        Connection = new AzureOpenAIConnection
        {
            Endpoint = endpoint,
            Credentials = credentials
        };
        OpenAIClient client = Connection.GetClient();
        _internalBatchRunner = new InternalBatchRunner(client.GetBatchClient(), client.GetOpenAIFileClient(), true);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public AzureOpenAIBatchRunner(AzureOpenAIConnection connection)
    {
        Connection = connection;
        OpenAIClient client = Connection.GetClient();
        _internalBatchRunner = new InternalBatchRunner(client.GetBatchClient(), client.GetOpenAIFileClient(), true);
    }

    /// <summary>
    /// Creates a new batch run.
    /// </summary>
    /// <param name="options">Options applied to every entry in the batch.</param>
    /// <param name="lines">The batch entries to submit.</param>
    /// <returns>The created batch run.</returns>
    public Task<ChatBatchRun> RunChatBatchAsync(ChatBatchOptions options, IList<ChatBatchRequest> lines)
    {
        return _internalBatchRunner.RunChatBatchAsync(options, lines);
    }

    /// <summary>
    /// Creates a new batch run with structured output for every line.
    /// </summary>
    /// <typeparam name="T">The structured output type returned for each line.</typeparam>
    /// <param name="options">Options applied to every entry in the batch.</param>
    /// <param name="lines">The batch entries to submit.</param>
    /// <param name="serializerOptions">Optional serializer options used for schema generation and result deserialization.</param>
    /// <returns>The created structured batch run.</returns>
    public Task<ChatBatchRun<T>> RunChatBatchAsync<T>(ChatBatchOptions options, IList<ChatBatchRequest> lines, JsonSerializerOptions? serializerOptions = null)
    {
        return _internalBatchRunner.RunChatBatchAsync<T>(options, lines, serializerOptions);
    }

    /// <summary>
    /// Gets an existing batch run.
    /// </summary>
    /// <param name="batchId">The batch identifier.</param>
    /// <returns>The batch run.</returns>
    public Task<ChatBatchRun> GetChatBatchAsync(string batchId)
    {
        return _internalBatchRunner.GetChatBatchAsync(batchId);
    }

    /// <summary>
    /// Gets an existing structured batch run.
    /// </summary>
    /// <typeparam name="T">The structured output type returned for each line.</typeparam>
    /// <param name="batchId">The batch identifier.</param>
    /// <param name="serializerOptions">Optional serializer options used for schema generation and result deserialization.</param>
    /// <returns>The structured batch run.</returns>
    public Task<ChatBatchRun<T>> GetChatBatchAsync<T>(string batchId, JsonSerializerOptions? serializerOptions = null)
    {
        return _internalBatchRunner.GetChatBatchAsync<T>(batchId, serializerOptions);
    }
}
