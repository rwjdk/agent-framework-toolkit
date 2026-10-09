using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.AzureOpenAI;
using Azure.Identity;

namespace AgentFrameworkToolkit.Tests.Providers;

[Trait("Category", "Live")]
public class AzureOpenAIDecisionFactoryLiveTests
{
    private const string Input = "Our production website is completely unavailable for every customer and cannot be used.";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateDecision_TextReturnsCompleteTypedAnswersAsync(bool useRbac)
    {
        IDecisionFactory factory = CreateFactory(useRbac);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleQuestions_ReturnEnumBooleanProbabilityAndScoreAsync(bool useRbac)
    {
        IDecisionFactory factory = CreateFactory(useRbac);
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

    private static AzureOpenAIDecisionFactory CreateFactory(bool useRbac)
    {
        string? endpoint = Environment.GetEnvironmentVariable("AzureDecisionEndpoint");
        string? deployment = Environment.GetEnvironmentVariable("AzureDecisionDeployment");
        Assert.False(string.IsNullOrWhiteSpace(endpoint), "Configure AzureDecisionEndpoint with the Foundry resource endpoint.");
        Assert.False(string.IsNullOrWhiteSpace(deployment), "Configure AzureDecisionDeployment with the deployed decision model name.");
        AzureOpenAIConnection connection;
        if (useRbac)
        {
            connection = new(endpoint!, new AzureCliCredential());
        }
        else
        {
            string? apiKey = Environment.GetEnvironmentVariable("AzureDecisionApiKey");
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "Configure AzureDecisionApiKey for API-key live tests.");
            connection = new(endpoint!, apiKey!);
        }
        connection.NetworkTimeout = TimeSpan.FromSeconds(60);
        return new(connection, deployment!);
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
