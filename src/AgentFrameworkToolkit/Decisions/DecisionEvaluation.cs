namespace AgentFrameworkToolkit.Decisions;

internal sealed record DecisionEvaluation(object[] Values, string Model, long InputTokens, long OutputTokens, long TotalTokens);
