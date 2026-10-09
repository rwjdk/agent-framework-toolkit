using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.Jev;
using Microsoft.Extensions.Configuration;
using Secrets;

namespace AgentFrameworkToolkit.Tests.Providers;

[Trait("Category", "Live")]
public class JevDecisionFactoryLiveTests
{
    private const string Input = "Our production website is completely unavailable for every customer and cannot be used.";

    [Fact]
    public async Task CreateDecision_TextReturnsCompleteTypedAnswersAsync()
    {
        IDecisionFactory factory = CreateFactory();
        DecisionResponse<SupportDecision> response = await factory.CreateDecisionAsync<SupportDecision>(
            new DecisionRequest { Input = Input }, TestContext.Current.CancellationToken);

        Assert.Equal(Category.Outage, response.Result.Category!.Value);
        Assert.Equal(2, response.Result.Category.Probabilities.Count);
        Assert.NotNull(response.Result.Available);
        Assert.InRange(response.Result.Available.Value, 0, 0.5);
        Assert.False(response.Result.Available.IsTrue);
        Assert.NotNull(response.Result.Severity);
        Assert.InRange(response.Result.Severity.Value, 0, 2);
        Assert.Equal(3, response.Result.Severity.Probabilities.Count);
        Assert.Equal(3, response.Result.Severity.LevelDescriptions.Count);
        Assert.NotEmpty(response.Model);
        Assert.True(response.InputTokenCount > 0);
        Assert.True(response.OutputTokenCount > 0);
        Assert.Equal(response.InputTokenCount + response.OutputTokenCount, response.TotalTokenCount);
    }

    [Fact]
    public async Task SingleQuestions_ReturnEnumBooleanProbabilityAndScoreAsync()
    {
        IDecisionFactory factory = CreateFactory();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Equal(Category.Outage, await factory.ChooseAsync<Category>(new ChoiceRequest
        {
            Input = Input, Question = "Is this report about an outage or billing?"
        }, cancellationToken));
        ProbabilityRequest request = new()
        {
            Input = Input, Question = "Is the website unavailable?", Threshold = 0.7
        };
        Assert.True(await factory.IsTrueAsync(request, cancellationToken));
        Probability probability = await factory.ProbabilityAsync(request, cancellationToken);
        Assert.InRange(probability.Value, 0.7, 1);
        Assert.Equal(0.7, probability.Threshold);
        Score<Severity> score = await factory.ScoreAsync<Severity>(new ScoreRequest
        {
            Input = Input, Question = "Rate impact: Cosmetic is appearance only, Degraded is usable with limitations, Blocking is unusable."
        }, cancellationToken);
        Assert.InRange(score.Value, 0, 2);
        Assert.Equal(3, score.Probabilities.Count);
        Assert.Equal(3, score.LevelDescriptions.Count);
    }

    private static JevDecisionFactory CreateFactory()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets<SecretsManager>().Build();
        string apiKey = Environment.GetEnvironmentVariable("TypeSafeApiKey") ?? configuration["TypeSafeApiKey"] ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(apiKey), "Configure TypeSafeApiKey in the environment or shared .NET user-secrets store.");
        // An explicit live-test model, matching the existing JevDotNet live setup.
        string model = Environment.GetEnvironmentVariable("JevDecisionModel") ?? "jev-latest";
        return new(new JevConnection(apiKey) { NetworkTimeout = TimeSpan.FromSeconds(60) }, model);
    }

    public class SupportDecision
    {
        [ChoiceQuestion<Category>("Is this report about an outage or billing?")]
        public Choice<Category>? Category { get; set; }

        [ProbabilityQuestion("Is the website working and available to customers?", threshold: 0.7)]
        public Probability? Available { get; set; }

        [ScoreQuestion<Severity>("Rate impact: Cosmetic is appearance only, Degraded is usable with limitations, Blocking is unusable.")]
        public Score<Severity>? Severity { get; set; }
    }

    public enum Category { Outage, Billing }
    public enum Severity { Cosmetic, Degraded, Blocking }
}
