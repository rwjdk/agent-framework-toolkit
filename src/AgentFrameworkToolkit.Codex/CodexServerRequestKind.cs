namespace AgentFrameworkToolkit.Codex;

/// <summary>The response Codex requires from the application.</summary>
internal enum CodexServerRequestKind
{
    /// <summary>A native request without a typed mapping.</summary>
    Other,
    /// <summary>Permission to execute a command.</summary>
    CommandApproval,
    /// <summary>Permission to apply file edits.</summary>
    FileChangeApproval,
    /// <summary>Answers to questions from Codex.</summary>
    UserInput
}
