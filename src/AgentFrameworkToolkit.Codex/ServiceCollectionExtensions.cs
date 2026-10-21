using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Dependency injection registration for Codex factories.</summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a factory using a Codex connection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">Authentication and runtime configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCodexAgentFactory(this IServiceCollection services, CodexConnection connection)
    {
        return services.AddSingleton(new CodexAgentFactory(connection));
    }

    /// <summary>Registers a factory using the existing Codex installation and login.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCodexAgentFactory(this IServiceCollection services)
    {
        return services.AddSingleton<CodexAgentFactory>();
    }

}
