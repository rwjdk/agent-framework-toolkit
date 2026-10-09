using AgentFrameworkToolkit.Decisions;
using Azure.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFrameworkToolkit.AzureOpenAI;

/// <summary>
/// Extension Methods for IServiceCollection
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a singleton Microsoft decision factory and the shared decision interface.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">Authentication, endpoint, and transport configuration.</param>
    /// <param name="deployment">The deployed Microsoft decision model's name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, AzureOpenAIConnection connection, string deployment)
    {
        services.AddSingleton(new AzureOpenAIDecisionFactory(connection, deployment));
        return services.AddSingleton<IDecisionFactory>(provider => provider.GetRequiredService<AzureOpenAIDecisionFactory>());
    }

    /// <summary>Registers Microsoft decisions with a Foundry resource endpoint and API key.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Foundry resource endpoint.</param>
    /// <param name="apiKey">The resource API key.</param>
    /// <param name="deployment">The deployed Microsoft decision model's name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, string endpoint, string apiKey, string deployment)
    {
        return services.AddAzureOpenAIDecisionFactory(new AzureOpenAIConnection(endpoint, apiKey), deployment);
    }

    /// <summary>Registers Microsoft decisions with a Foundry resource endpoint and RBAC credentials.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Foundry resource endpoint.</param>
    /// <param name="credentials">Credentials authorized to call the deployment.</param>
    /// <param name="deployment">The deployed Microsoft decision model's name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, string endpoint, TokenCredential credentials, string deployment)
    {
        return services.AddAzureOpenAIDecisionFactory(new AzureOpenAIConnection(endpoint, credentials), deployment);
    }

    /// <summary>
    /// Register an AzureOpenAIAgentFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="connection">Connection Details</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIAgentFactory(this IServiceCollection services, AzureOpenAIConnection connection)
    {
        return services.AddSingleton(new AzureOpenAIAgentFactory(connection));
    }

    /// <summary>
    /// Register an AzureOpenAIAgentFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="endpoint">Your Azure OpenAI Endpoint</param>
    /// <param name="apiKey">The API Key</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIAgentFactory(this IServiceCollection services, string endpoint, string apiKey)
    {
        return services.AddSingleton(new AzureOpenAIAgentFactory(endpoint, apiKey));
    }

    /// <summary>
    /// Register an AzureOpenAIAgentFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="endpoint">You Azure OpenAI Endpoint</param>
    /// <param name="credentials">Your RBAC Credentials</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIAgentFactory(this IServiceCollection services, string endpoint, TokenCredential credentials)
    {
        return services.AddSingleton(new AzureOpenAIAgentFactory(endpoint, credentials));
    }

    /// <summary>
    /// Register an AzureOpenAIEmbeddingFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="connection">Connection Details</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIEmbeddingFactory(this IServiceCollection services, AzureOpenAIConnection connection)
    {
        return services.AddSingleton(new AzureOpenAIEmbeddingFactory(connection));
    }

    /// <summary>
    /// Register an AzureOpenAIEmbeddingFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="endpoint">Your Azure OpenAI Endpoint</param>
    /// <param name="apiKey">The API Key</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIEmbeddingFactory(this IServiceCollection services, string endpoint, string apiKey)
    {
        return services.AddSingleton(new AzureOpenAIEmbeddingFactory(endpoint, apiKey));
    }

    /// <summary>
    /// Register an AzureOpenAIEmbeddingFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="endpoint">Your Azure OpenAI Endpoint</param>
    /// <param name="credentials">Your RBAC Credentials</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddAzureOpenAIEmbeddingFactory(this IServiceCollection services, string endpoint, TokenCredential credentials)
    {
        return services.AddSingleton(new AzureOpenAIEmbeddingFactory(endpoint, credentials));
    }
}
