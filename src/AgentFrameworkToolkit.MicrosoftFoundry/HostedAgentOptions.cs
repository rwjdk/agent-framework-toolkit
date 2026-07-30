using JetBrains.Annotations;
using Microsoft.Extensions.AI;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Options for a Hosted Agent
/// </summary>
[PublicAPI]
public class HostedAgentOptions
{
    /// <summary>
    /// The Unique Name of the Agent
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// An Action, if set, will apply Tool Calling Middleware so you can inspect Tool Call Details
    /// </summary>
    public Action<ToolCallingDetails>? RawToolCallDetails { get; set; }

    /// <summary>
    /// Enable Tool Calling Middleware allowing you to inspect, manipulate and cancel a tool-call
    /// </summary>
    public MiddlewareDelegates.ToolCallingMiddlewareDelegate? ToolCallingMiddleware { get; set; }

    /// <summary>
    /// Enable OpenTelemetry Middleware for OpenTelemetry Logging
    /// </summary>
    public OpenTelemetryMiddleware? OpenTelemetryMiddleware { get; set; }

    /// <summary>
    /// Enable Logging Middleware for custom Logging
    /// </summary>
    public LoggingMiddleware? LoggingMiddleware { get; set; }

    /// <summary>
    /// An optional <see cref="IServiceProvider"/> to use for resolving services required by the <see cref="AIFunction"/> instances being invoked.
    /// </summary>
    public IServiceProvider? Services { get; set; }
}