using Microsoft.Extensions.AI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;
using Secrets;

namespace AgentFrameworkToolkit.Tests.Providers;

public class OpenAIDecisionFactoryLiveTests
{
    [Fact]
    [Trait("Category", "Live")]
    public async Task SingleQuestion_ReturnsEnumBooleanProbabilityAndScoreAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenAIDecisionFactory factory = new(secrets.OpenAiApiKey, OpenAIChatModels.Gpt6Luna);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string input = "The screen arrived shattered.";
        Assert.Equal(Category.Damage, await factory.ChooseAsync<Category>(new ChoiceRequest { Input = input, Question = "Is this about damage or billing?" }, cancellationToken));
        Assert.True(await factory.IsTrueAsync(new ProbabilityRequest { Input = input, Question = "Does the customer report damage?" }, cancellationToken));
        Probability probability = await factory.ProbabilityAsync(new ProbabilityRequest { Input = input, Question = "Does the customer report damage?" }, cancellationToken);
        Assert.InRange(probability.Value, 0.5, 1);
        Score<Severity> score = await factory.ScoreAsync<Severity>(new ScoreRequest
        {
            Input = input,
            Question = "Rate the impact: Cosmetic is appearance only, Degraded means usable with limitations, Blocking means unusable."
        }, cancellationToken);
        Assert.InRange(score.Value, 0, 2);
        Assert.Equal(3, score.Probabilities.Count);
        Assert.Equal(3, score.LevelDescriptions.Count);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task CreateDecision_ImageOnlyReturnsProbabilityAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenAIDecisionFactory factory = new(secrets.OpenAiApiKey, OpenAIChatModels.Gpt6Luna);
        // A small PNG keeps the fixture self-contained; this checks API compatibility, not vision quality.
        DataContent image = new(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAAJklEQVR4nO3NMQ0AAAwDoPo33arYsQQMkB6LQCAQCAQCgUAg+BIMi1X0pjxKe0gAAAAASUVORK5CYII="), "image/png");

        OpenAIDecisionResponse<ImageDecision> response = await factory.CreateDecisionAsync<ImageDecision>(
            new DecisionImageRequest { Images = [image], ImageDetail = ImageDetail.Low },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response.Result.HasText);
        Assert.InRange(response.Result.HasText.Value, 0, 1);
        Assert.True(response.InputTokenCount > 0);
        Assert.NotEmpty(response.Model);
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task CreateDecision_TextReturnsCompleteTypedAnswersAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        OpenAIDecisionFactory factory = new(new OpenAIConnection(secrets.OpenAiApiKey)
        {
            NetworkTimeout = TimeSpan.FromSeconds(60)
        }, OpenAIChatModels.Gpt6Luna);

        OpenAIDecisionResponse<SupportDecision> response = await factory.CreateDecisionAsync<SupportDecision>(
            new DecisionRequest { Input = "The product arrived with a completely shattered screen and cannot be used." },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.Result.Damaged);
        Assert.NotNull(response.Result.Category);
        Assert.Equal(Category.Damage, response.Result.Category.Value);
        Assert.Equal(2, response.Result.Category.Probabilities.Count);
        Assert.NotNull(response.Result.Severity);
        Assert.InRange(response.Result.Severity.Value, 0, 2);
        Assert.Equal(3, response.Result.Severity.Probabilities.Count);
        Assert.Equal(3, response.Result.Severity.LevelDescriptions.Count);
        Assert.True(response.InputTokenCount > 0);
        Assert.True(response.TotalTokenCount >= response.InputTokenCount);
        Assert.NotEmpty(response.Model);
    }

    public class SupportDecision
    {
        [ProbabilityQuestion("Does the customer explicitly report physical damage to the product?")]
        public bool Damaged { get; set; }

        [ChoiceQuestion<Category>("Is the report about physical damage or a billing question?")]
        public Choice<Category>? Category { get; set; }

        [ScoreQuestion<Severity>("Rate impact: Cosmetic is appearance only, Degraded is usable with limitations, Blocking is unusable.")]
        public Score<Severity>? Severity { get; set; }
    }

    public class ImageDecision
    {
        [ProbabilityQuestion("Does the image contain readable text?")]
        public Probability? HasText { get; set; }
    }

    public enum Category { Damage, Billing }
    public enum Severity { Cosmetic, Degraded, Blocking }
}
