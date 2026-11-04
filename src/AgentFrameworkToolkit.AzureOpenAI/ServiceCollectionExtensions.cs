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
    /// <summary>Registers a singleton Azure decision factory and the shared decision interface.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">Authentication, endpoint, and transport configuration.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, AzureOpenAIConnection connection, DecisionProtocol protocol, string deployment)
    {
        services.AddSingleton(new AzureOpenAIDecisionFactory(connection, protocol, deployment));
        return services.AddSingleton<IDecisionFactory>(provider => provider.GetRequiredService<AzureOpenAIDecisionFactory>());
    }

    /// <summary>Registers Azure decisions with an Azure resource endpoint and API key.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Azure resource endpoint.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="apiKey">The resource API key.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, string endpoint, DecisionProtocol protocol, string apiKey, string deployment)
    {
        return services.AddAzureOpenAIDecisionFactory(new AzureOpenAIConnection(endpoint, apiKey), protocol, deployment);
    }

    /// <summary>Registers Azure decisions with an Azure resource endpoint and RBAC credentials.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Azure resource endpoint.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="credentials">Credentials authorized to call the deployment.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureOpenAIDecisionFactory(this IServiceCollection services, string endpoint, DecisionProtocol protocol, TokenCredential credentials, string deployment)
    {
        return services.AddAzureOpenAIDecisionFactory(new AzureOpenAIConnection(endpoint, credentials), protocol, deployment);
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
