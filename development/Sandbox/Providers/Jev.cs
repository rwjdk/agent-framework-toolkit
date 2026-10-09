using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.Jev;

namespace Sandbox.Providers;

public static class Jev
{
    public static async Task RunAsync(string apiKey, string model, CancellationToken cancellationToken = default)
    {
        IDecisionFactory factory = new JevDecisionFactory(apiKey, model);
        Probability probability = await factory.ProbabilityAsync(new ProbabilityRequest
        {
            Input = "The screen arrived shattered.",
            Question = "Is the product damaged?",
            Threshold = 0.7
        }, cancellationToken);
        Console.WriteLine($"Damage probability: {probability.Value}; damaged: {probability.IsTrue}");
    }
}
