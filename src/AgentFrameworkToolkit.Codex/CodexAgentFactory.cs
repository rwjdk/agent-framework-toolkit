using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Creates Codex agents backed by the local app-server protocol.</summary>
[PublicAPI]
public class CodexAgentFactory
{
    /// <summary>The authentication and runtime connection used to create clients.</summary>
    public CodexConnection Connection { get; }

    /// <summary>Creates a factory using the existing Codex installation and login.</summary>
    public CodexAgentFactory() : this(new CodexConnection())
    {
    }

    /// <summary>Creates a factory using a connection.</summary>
    /// <param name="connection">Authentication and runtime configuration.</param>
    public CodexAgentFactory(CodexConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
    }

    /// <summary>Creates an agent with default settings in the specified working directory.</summary>
    /// <param name="workingDirectory">The required workspace directory.</param>
    /// <returns>An asynchronously disposable Codex agent.</returns>
    public CodexAgent CreateAgent(string workingDirectory)
    {
        return CreateAgent(new CodexAgentOptions { WorkingDirectory = workingDirectory });
    }

    /// <summary>Creates an agent that starts Codex on first use and owns its client.</summary>
    /// <param name="options">Agent configuration.</param>
    /// <returns>An asynchronously disposable Codex agent.</returns>
    public CodexAgent CreateAgent(CodexAgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WorkingDirectory);
        return new CodexAgent(Connection.GetClient(), options, ownsClient: true);
    }
}

