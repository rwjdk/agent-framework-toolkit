using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>
/// Extension Methods for IServiceCollection
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>Registers an OpenAIDecisionFactory as a singleton.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">OpenAI connection configuration.</param>
    /// <returns>The service collection.</returns>
    /// <param name="model">The model used for decisions.</param>
    public static IServiceCollection AddOpenAIDecisionFactory(this IServiceCollection services, OpenAIConnection connection, string model)
    {
        return services.AddSingleton(new OpenAIDecisionFactory(connection, model));
    }

    /// <summary>Registers an OpenAIDecisionFactory as a singleton.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>The service collection.</returns>
    /// <param name="model">The model used for decisions.</param>
    public static IServiceCollection AddOpenAIDecisionFactory(this IServiceCollection services, string apiKey, string model)
    {
        return services.AddSingleton(new OpenAIDecisionFactory(apiKey, model));
    }

    /// <summary>
    /// Register an OpenAIAgentFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="connection">Connection Details</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddOpenAIAgentFactory(this IServiceCollection services, OpenAIConnection connection)
    {
        return services.AddSingleton(new OpenAIAgentFactory(connection));
    }

    /// <summary>
    /// Register an OpenAIAgentFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="apiKey">The API Key</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddOpenAIAgentFactory(this IServiceCollection services, string apiKey)
    {
        return services.AddSingleton(new OpenAIAgentFactory(apiKey));
    }

    /// <summary>
    /// Register an OpenAIEmbeddingFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="connection">Connection Details</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddOpenAIEmbeddingFactory(this IServiceCollection services, OpenAIConnection connection)
    {
        return services.AddSingleton(new OpenAIEmbeddingFactory(connection));
    }

    /// <summary>
    /// Register an OpenAIEmbeddingFactory as a Singleton
    /// </summary>
    /// <param name="services">The IServiceCollection collection</param>
    /// <param name="apiKey">The API Key</param>
    /// <returns>The ServiceCollection</returns>
    public static IServiceCollection AddOpenAIEmbeddingFactory(this IServiceCollection services, string apiKey)
    {
        return services.AddSingleton(new OpenAIEmbeddingFactory(apiKey));
    }
}
