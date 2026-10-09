using AgentFrameworkToolkit.Decisions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFrameworkToolkit.Jev;

/// <summary>Registers Jev decision services.</summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a singleton Jev factory and the shared decision interface.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The TypeSafe AI API key.</param>
    /// <param name="model">The model used for decisions.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJevDecisionFactory(this IServiceCollection services, string apiKey, string model)
    {
        return services.AddJevDecisionFactory(new JevConnection(apiKey), model);
    }

    /// <summary>Registers a singleton Jev factory and the shared decision interface.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">Authentication, endpoint, and transport configuration.</param>
    /// <param name="model">The model used for decisions.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJevDecisionFactory(this IServiceCollection services, JevConnection connection, string model)
    {
        services.AddSingleton(new JevDecisionFactory(connection, model));
        return services.AddSingleton<IDecisionFactory>(provider => provider.GetRequiredService<JevDecisionFactory>());
    }
}
