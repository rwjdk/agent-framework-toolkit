using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Specifies the image detail level used by the Decisions API.</summary>
[PublicAPI]
public enum ImageDetail
{
    /// <summary>Lets the model select the image detail level.</summary>
    Auto,

    /// <summary>Uses low image detail.</summary>
    Low,

    /// <summary>Uses high image detail.</summary>
    High,

    /// <summary>Uses original image detail.</summary>
    Original
}
