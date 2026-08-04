using JetBrains.Annotations;

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Option for creating a new Hosted Agent
/// </summary>
[PublicAPI]
public class HostedAgentCreationOptions
{
    /// <summary>
    /// The Unique Name of the Agent
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The .NET runtime (Example : dotnet_10)
    /// </summary>
    public required string DotNetRuntime { get; set; }

    /// <summary>
    /// The CPU to use in hosting session (values: "0.5", "1" or "2")
    /// </summary>
    public required string Cpu { get; set; }

    /// <summary>
    /// The Memory to use in hosting session (values: "1Gi", "2Gi" or "4Gi")
    /// </summary>
    public required string Memory { get; set; }

    /// <summary>
    /// The C# source-code directory of your hosted Agent
    /// </summary>
    public required string SourceDirectory { get; set; }

    /// <summary>
    /// The DLL Assembly Name of your hosted Agent
    /// </summary>
    public required string AssemblyName { get; set; }
}