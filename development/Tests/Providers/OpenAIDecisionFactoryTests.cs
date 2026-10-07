using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Tests.Providers;

public class OpenAIDecisionFactoryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ScoreAsync_MapsDetailsOrThrowsOnRefusalAsync(bool useImages, bool refused)
    {
        JsonObject response = CreateResponse();
        JsonNode answer = refused
            ? new JsonObject { ["name"] = "Value", ["type"] = "refusal" }
            : response["answers"]![2]!.DeepClone();
        answer["name"] = "Value";
        response["answers"] = new JsonArray(answer);
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        Func<Task<Score<Level>>> call = useImages
            ? () => factory.ScoreAsync<Level>(new ScoreImageRequest { Question = "Rate damage", Images = [new(new byte[] { 1 }, "image/png")], ImageDetail = ImageDetail.High, SafetyIdentifier = "score-user" }, TestContext.Current.CancellationToken)
            : () => factory.ScoreAsync<Level>(new ScoreRequest { Question = "Rate damage", Input = "Evidence", SafetyIdentifier = "score-user" }, TestContext.Current.CancellationToken);
        if (refused)
        {
            DecisionRefusalException exception = await Assert.ThrowsAsync<DecisionRefusalException>(call);
            Assert.Equal("Value", exception.QuestionName);
        }
        else
        {
            Score<Level> result = await call();
            Assert.Equal(0.7, result.Value);
            Assert.Equal(0.6, result.Confidence);
            Assert.Equal(0.3, result.Probabilities[Level.Low]);
            Assert.Equal("Minor impact", result.LevelDescriptions[Level.Low]);
        }
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement question = request.RootElement.GetProperty("questions")[0];
        Assert.Equal("score", question.GetProperty("type").GetString());
        Assert.Equal("Rate damage", question.GetProperty("instructions").GetString());
        Assert.Equal("Low", question.GetProperty("levels")[0].GetProperty("label").GetString());
        Assert.Equal("score-user", request.RootElement.GetProperty("safety_identifier").GetString());
        if (useImages)
        {
            Assert.Equal("high", request.RootElement.GetProperty("input")[0].GetProperty("content")[0].GetProperty("detail").GetString());
        }
    }

    [Fact]
    public async Task ScoreAsync_ValidatesEvidenceAndQuestionBeforeSendingAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ScoreAsync<Level>(new ScoreRequest { Question = " ", Input = "Evidence" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ScoreAsync<Level>(new ScoreImageRequest { Question = "Rate", Images = [], Input = "Evidence" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.ScoreAsync<Level>((ScoreRequest)null!, TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("iVBORw0KGgo=", "image/png")]
    [InlineData("/9j/", "image/jpeg")]
    [InlineData("R0lGODlh", "image/gif")]
    [InlineData("UklGRgAAAABXRUJQ", "image/webp")]
    public async Task ImageRequest_EncodesDataContentAsync(string image, string mediaType)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await factory.CreateDecisionAsync<DetailedResult>(new DecisionImageRequest { Images = [new DataContent(Convert.FromBase64String(image), mediaType)] }, TestContext.Current.CancellationToken);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal($"data:{mediaType};base64,{image}", request.RootElement.GetProperty("input")[0].GetProperty("content")[0].GetProperty("image_url").GetString());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Probability_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probability(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probability(value, 0.7));
    }

    [Fact]
    public void DetailedResults_CopyAndProtectDictionaries()
    {
        Dictionary<Level, double> probabilities = new() { [Level.Low] = 0.2, [Level.High] = 0.8 };
        Dictionary<Level, string> descriptions = new() { [Level.Low] = "Low", [Level.High] = "High" };
        Choice<Level> choice = new(Level.High, 0.8, probabilities);
        Score<Level> score = new(0.8, 0.8, probabilities, descriptions);
        probabilities[Level.High] = 0;
        descriptions[Level.High] = "Changed";
        Assert.Equal(0.8, choice.Probabilities[Level.High]);
        Assert.Equal(0.8, score.Probabilities[Level.High]);
        Assert.Equal("High", score.LevelDescriptions[Level.High]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Level, double>)choice.Probabilities)[Level.High] = 0);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Level, double>)score.Probabilities)[Level.High] = 0);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Level, string>)score.LevelDescriptions)[Level.High] = "Changed");
    }

    [Fact]
    public async Task CreateDecision_ProbabilityUsesAttributeThresholdAsync()
    {
        JsonObject response = CreateResponse();
        response["answers"] = new JsonArray(response["answers"]![0]!.DeepClone());
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        OpenAIDecisionResponse<ThresholdResult> result = await factory.CreateDecisionAsync<ThresholdResult>(new DecisionRequest { Input = "Evidence" }, TestContext.Current.CancellationToken);
        Assert.Equal(0.8, result.Result.Damaged!.Threshold);
        Assert.False(result.Result.Damaged.IsTrue);
    }

    [Theory]
    [InlineData(false, 0.7, true)]
    [InlineData(true, 0.7, true)]
    [InlineData(false, 0.8, false)]
    [InlineData(true, 0.8, false)]
    public async Task Probability_UsesRequestThresholdAsync(bool useImages, double threshold, bool expected)
    {
        JsonObject response = CreateResponse();
        JsonNode answer = response["answers"]![0]!.DeepClone();
        answer["name"] = "Value";
        response["answers"] = new JsonArray(answer);
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        Probability result = useImages
            ? await factory.ProbabilityAsync(new ProbabilityImageRequest { Question = "Damaged?", Images = [new DataContent(new byte[] { 1 }, "image/png")], Threshold = threshold }, TestContext.Current.CancellationToken)
            : await factory.ProbabilityAsync(new ProbabilityRequest { Question = "Damaged?", Input = "Evidence", Threshold = threshold }, TestContext.Current.CancellationToken);
        Assert.Equal(0.7, result.Value);
        Assert.Equal(threshold, result.Threshold);
        Assert.Equal(expected, result.IsTrue);
        Assert.True(result.IsAtLeast(0.6));
        bool isTrue = useImages
            ? await factory.IsTrueAsync(new ProbabilityImageRequest { Question = "Damaged?", Images = [new DataContent(new byte[] { 1 }, "image/png")], Threshold = threshold }, TestContext.Current.CancellationToken)
            : await factory.IsTrueAsync(new ProbabilityRequest { Question = "Damaged?", Input = "Evidence", Threshold = threshold }, TestContext.Current.CancellationToken);
        Assert.Equal(expected, isTrue);
    }

    [Theory]
    [InlineData(false, OpenAIChatModels.Gpt6Luna, null, OpenAIChatModels.Gpt6Luna)]
    [InlineData(true, OpenAIChatModels.Gpt6Luna, null, OpenAIChatModels.Gpt6Luna)]
    [InlineData(false, OpenAIChatModels.Gpt6Luna, "explicit-model", "explicit-model")]
    [InlineData(true, OpenAIChatModels.Gpt6Luna, "explicit-model", "explicit-model")]
    [InlineData(false, OpenAIChatModels.Gpt6Sol, OpenAIChatModels.Gpt6Luna, OpenAIChatModels.Gpt6Luna)]
    [InlineData(true, OpenAIChatModels.Gpt6Sol, OpenAIChatModels.Gpt6Luna, OpenAIChatModels.Gpt6Luna)]
    public async Task AgentDecisions_UsesKnownFallbackOrExplicitOverrideAsync(bool useClientExtension, string agentModel, string? decisionModel, string expectedModel)
    {
        using RecordingHandler handler = new();
        using HttpClient httpClient = new(handler);
        OpenAIConnection connection = CreateConnection(httpClient);
        AgentFrameworkToolkit.OpenAI.AgentOptions options = new() { Model = agentModel, DecisionApiModel = decisionModel };
        OpenAIAgent agent = useClientExtension
            ? Assert.IsType<OpenAIAgent>(connection.GetClient().AsAIAgent(options))
            : new OpenAIAgentFactory(connection).CreateAgent(options);
        await agent.Decisions.CreateDecisionAsync<DetailedResult>(new DecisionRequest
        {
            Input = "Evidence"
        }, TestContext.Current.CancellationToken);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(expectedModel, request.RootElement.GetProperty("model").GetString());
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData(OpenAIChatModels.Gpt6Sol)]
    [InlineData("gpt-6-luna-unverified-version")]
    public void AgentDecisions_DoesNotInferSupportForUnknownModels(string model)
    {
        OpenAIAgent agent = new OpenAIAgentFactory("fake-key").CreateAgent(model);
        Assert.Throws<InvalidOperationException>(() => agent.Decisions);
    }

    [Fact]
    public void AgentDecisions_RequiresExplicitModel()
    {
        OpenAIAgent agent = new OpenAIAgentFactory("fake-key").CreateAgent(OpenAIChatModels.Gpt6Sol);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => agent.Decisions);
        Assert.Contains("DecisionApiModel", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void DecisionFactory_RejectsMissingModel(string? model)
    {
        Assert.ThrowsAny<ArgumentException>(() => new OpenAIDecisionFactory("fake-key", model!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Decisions_RetainsClientAcrossRepeatedRequestsAsync(bool attachedToAgent)
    {
        using RecordingHandler handler = new();
        using HttpClient httpClient = new(handler);
        int clientConfigurations = 0;
        OpenAIConnection connection = new("fake-key")
        {
            Endpoint = "https://example.test/custom/v1",
            AdditionalOpenAIClientOptions = options =>
            {
                clientConfigurations++;
                options.Transport = new HttpClientPipelineTransport(httpClient);
            }
        };
        OpenAIDecisionFactory decisions;
        if (attachedToAgent)
        {
            OpenAIAgent agent = new OpenAIAgentFactory(connection).CreateAgent(new AgentFrameworkToolkit.OpenAI.AgentOptions { Model = OpenAIChatModels.Gpt6Sol, DecisionApiModel = OpenAIChatModels.Gpt6Luna });
            decisions = agent.Decisions;
            Assert.Same(decisions, agent.Decisions);
        }
        else
        {
            decisions = new(connection, OpenAIChatModels.Gpt6Luna);
        }
        List<RawCallDetails> rawCalls = [];
        for (int i = 0; i < 3; i++)
        {
            await decisions.CreateDecisionAsync<DetailedResult>(new DecisionRequest
            {
                Input = "Evidence",
                RawHttpCallDetails = i == 1 ? rawCalls.Add : null
            }, TestContext.Current.CancellationToken);
        }
        Assert.Equal(1, clientConfigurations);
        Assert.Equal(3, handler.RequestCount);
        RawCallDetails rawCall = Assert.Single(rawCalls);
        Assert.Equal("https://example.test/custom/v1/decisions", rawCall.RequestUrl);
        Assert.Contains("Evidence", rawCall.RequestData);
        Assert.Contains("answers", rawCall.ResponseData);
        Assert.Contains("\n  \"model\":", rawCall.RequestData);
        Assert.Contains("\n  \"model\":", rawCall.ResponseData);
    }

    [Fact]
    public async Task Decisions_ClientExtensionSharesSuppliedSdkClientAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient httpClient = new(handler);
        global::OpenAI.OpenAIClient sdkClient = CreateConnection(httpClient).GetClient();
        OpenAIAgent agent = Assert.IsType<OpenAIAgent>(sdkClient.AsAIAgent(new AgentFrameworkToolkit.OpenAI.AgentOptions
        {
            Model = OpenAIChatModels.Gpt6Sol,
            DecisionApiModel = OpenAIChatModels.Gpt6Luna
        }));
        await agent.Decisions.CreateDecisionAsync<DetailedResult>(new DecisionRequest
        {
            Input = "Evidence"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("choice", false)]
    [InlineData("choice", true)]
    [InlineData("probability", false)]
    [InlineData("probability", true)]
    [InlineData("boolean", false)]
    [InlineData("boolean", true)]
    public async Task SingleQuestion_ReturnsSimpleValueAndSendsQuestionAsync(string method, bool useImages)
    {
        JsonObject response = CreateResponse();
        JsonNode answer = response["answers"]![method == "choice" ? 1 : 0]!.DeepClone();
        answer["name"] = "Value";
        response["answers"] = new JsonArray(answer);
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        DataContent[] images = [new(new byte[] { 1 }, "image/png")];
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string question = "The runtime question";
        if (method == "choice")
        {
            Level result = await (useImages
                ? factory.ChooseAsync<Level>(new ChoiceImageRequest { Input = "Context", Question = question, Images = images, ImageDetail = ImageDetail.High, SafetyIdentifier = "single-user" }, cancellationToken)
                : factory.ChooseAsync<Level>(new ChoiceRequest { Input = "Context", Question = question, SafetyIdentifier = "single-user" }, cancellationToken));
            Assert.Equal(Level.High, result);
        }
        else if (method == "probability")
        {
            Probability result = await (useImages
                ? factory.ProbabilityAsync(new ProbabilityImageRequest { Input = "Context", Question = question, Images = images, ImageDetail = ImageDetail.High, SafetyIdentifier = "single-user" }, cancellationToken)
                : factory.ProbabilityAsync(new ProbabilityRequest { Input = "Context", Question = question, SafetyIdentifier = "single-user" }, cancellationToken));
            Assert.Equal(0.7, result.Value);
        }
        else
        {
            bool result = await (useImages
                ? factory.IsTrueAsync(new ProbabilityImageRequest { Input = "Context", Question = question, Images = images, ImageDetail = ImageDetail.High, SafetyIdentifier = "single-user", Threshold = 0.7 }, cancellationToken)
                : factory.IsTrueAsync(new ProbabilityRequest { Input = "Context", Question = question, SafetyIdentifier = "single-user", Threshold = 0.7 }, cancellationToken));
            Assert.True(result);
        }
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement root = request.RootElement;
        Assert.Equal(1, root.GetProperty("questions").GetArrayLength());
        Assert.Equal(question, root.GetProperty("questions")[0].GetProperty("instructions").GetString());
        Assert.Equal("single-user", root.GetProperty("safety_identifier").GetString());
        if (useImages)
        {
            Assert.Equal("Context", root.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString());
            Assert.Equal("high", root.GetProperty("input")[0].GetProperty("content")[1].GetProperty("detail").GetString());
        }
    }

    [Theory]
    [InlineData("choice")]
    [InlineData("probability")]
    [InlineData("boolean")]
    public async Task SingleQuestion_RefusalThrowsAsync(string method)
    {
        JsonObject response = CreateResponse();
        response["answers"] = new JsonArray(new JsonObject { ["type"] = "refusal", ["name"] = "Value" });
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Func<Task> action = method switch
        {
            "choice" => () => factory.ChooseAsync<Level>(new ChoiceRequest { Input = "Context", Question = "Question" }, cancellationToken),
            "boolean" => () => factory.IsTrueAsync(new ProbabilityRequest { Input = "Context", Question = "Question" }, cancellationToken),
            _ => () => factory.ProbabilityAsync(new ProbabilityRequest { Input = "Context", Question = "Question" }, cancellationToken)
        };
        DecisionRefusalException exception = await Assert.ThrowsAsync<DecisionRefusalException>(action);
        Assert.Equal("Value", exception.QuestionName);
    }

    [Fact]
    public async Task SingleQuestion_ValidatesQuestionAndThresholdBeforeSendingAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ChooseAsync<Level>(new ChoiceRequest { Input = "Context", Question = " " }, cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ProbabilityAsync(new ProbabilityRequest { Input = "Context", Question = " " }, cancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.IsTrueAsync(new ProbabilityRequest { Input = "Context", Question = "Question", Threshold = double.NaN }, cancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SingleQuestion_ImageOnlyRequestOmitsTextAsync()
    {
        JsonObject response = CreateResponse();
        JsonNode answer = response["answers"]![0]!.DeepClone();
        answer["name"] = "Value";
        response["answers"] = new JsonArray(answer);
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        Probability probability = await factory.ProbabilityAsync(new ProbabilityImageRequest
        {
            Question = "Is it damaged?", Images = [new DataContent(new byte[] { 1 }, "image/png")]
        }, TestContext.Current.CancellationToken);
        Assert.Equal(0.7, probability.Value);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement content = request.RootElement.GetProperty("input")[0].GetProperty("content");
        Assert.Equal(1, content.GetArrayLength());
        Assert.Equal("input_image", content[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task SingleQuestion_RejectsMissingEvidenceAndNullRequestBeforeSendingAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ChooseAsync<Level>(new ChoiceRequest { Question = "Question", Input = "" }, cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ChooseAsync<Level>(new ChoiceImageRequest { Question = "Question", Images = [], Input = "Text cannot replace the required images" }, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.ProbabilityAsync(new ProbabilityImageRequest { Question = "Question", Images = null! }, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.ProbabilityAsync((ProbabilityRequest)null!, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.IsTrueAsync((ProbabilityRequest)null!, cancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task CreateDecision_TextMapsDetailedAnswersAndUsageAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIConnection connection = CreateConnection(client);
        OpenAIDecisionFactory factory = new(connection, "custom-model");

        OpenAIDecisionResponse<DetailedResult> response = await factory.CreateDecisionAsync<DetailedResult>(new DecisionRequest
        {
            Input = "Evidence",
            SafetyIdentifier = "user-123"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(connection, factory.Connection);
        Assert.Equal(0.7, response.Result.Damaged!.Value);
        Assert.True(response.Result.Damaged.IsTrue);
        Assert.False(response.Result.Damaged.IsAtLeast(0.8));
        Assert.Equal(Level.High, response.Result.Category!.Value);
        Assert.Equal(0.8, response.Result.Category.Confidence);
        Assert.Equal(0.2, response.Result.Category.Probabilities[Level.Low]);
        Assert.Equal(0.7, response.Result.Severity!.Value);
        Assert.Equal(0.6, response.Result.Severity.Confidence);
        Assert.Equal(0.7, response.Result.Severity.Probabilities[Level.High]);
        Assert.Equal("Minor impact", response.Result.Severity.LevelDescriptions[Level.Low]);
        Assert.Equal("High", response.Result.Severity.LevelDescriptions[Level.High]);
        Assert.Equal("returned-model", response.Model);
        Assert.Equal(10, response.InputTokenCount);
        Assert.Equal(2, response.OutputTokenCount);
        Assert.Equal(12, response.TotalTokenCount);

        Assert.Equal("https://example.test/custom/v1/decisions", handler.RequestUri!.AbsoluteUri);
        Assert.Equal("POST", handler.Method);
        Assert.Equal("Bearer fake-key", handler.Authorization);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement root = request.RootElement;
        Assert.Equal("Evidence", root.GetProperty("input").GetString());
        Assert.Equal("custom-model", root.GetProperty("model").GetString());
        Assert.Equal("user-123", root.GetProperty("safety_identifier").GetString());
        JsonElement questions = root.GetProperty("questions");
        Assert.Equal("predicate", questions[0].GetProperty("type").GetString());
        Assert.Equal("Damaged", questions[0].GetProperty("name").GetString());
        Assert.Equal("Is it damaged?", questions[0].GetProperty("instructions").GetString());
        Assert.Equal("Low", questions[1].GetProperty("choices")[0].GetProperty("value").GetString());
        Assert.Equal("Minor impact", questions[1].GetProperty("choices")[0].GetProperty("description").GetString());
        Assert.Equal("Low", questions[2].GetProperty("levels")[0].GetProperty("label").GetString());
    }

    [Fact]
    public async Task CreateDecision_MapsSimpleAndNullablePropertiesAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        OpenAIDecisionResponse<SimpleResult> response = await factory.CreateDecisionAsync<SimpleResult>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.Result.Damaged);
        Assert.Equal(Level.High, response.Result.Category);
        Assert.Equal(0.7m, response.Result.Severity);

        OpenAIDecisionResponse<NumericResult> numeric = await factory.CreateDecisionAsync<NumericResult>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0.7m, numeric.Result.Damaged);
        Assert.Equal(Level.High, numeric.Result.Category);
        Assert.Equal(0.7, numeric.Result.Severity);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("gpt-6-luna", request.RootElement.GetProperty("model").GetString());
        Assert.False(request.RootElement.TryGetProperty("safety_identifier", out _));
    }

    [Theory]
    [InlineData(ImageDetail.Auto, "auto")]
    [InlineData(ImageDetail.Low, "low")]
    [InlineData(ImageDetail.High, "high")]
    [InlineData(ImageDetail.Original, "original")]
    public async Task CreateDecision_ImagesSerializeBytesAndDetailWithoutTextAsync(ImageDetail detail, string wireValue)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await factory.CreateDecisionAsync<DetailedResult>(new DecisionImageRequest { Images = [new DataContent(new byte[] { 1, 2, 3 }, "image/png")], ImageDetail = detail }, cancellationToken: TestContext.Current.CancellationToken);

        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement message = request.RootElement.GetProperty("input")[0];
        Assert.Equal("user", message.GetProperty("role").GetString());
        JsonElement content = message.GetProperty("content");
        Assert.Equal(1, content.GetArrayLength());
        Assert.Equal("input_image", content[0].GetProperty("type").GetString());
        Assert.Equal("data:image/png;base64,AQID", content[0].GetProperty("image_url").GetString());
        Assert.Equal(wireValue, content[0].GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CreateDecision_ImagesIncludeOptionalContextAndAcceptDataUrisAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await factory.CreateDecisionAsync<DetailedResult>(new DecisionImageRequest { Images = [new DataContent(new byte[] { 1 }, "image/png")], Input = "Context" }, cancellationToken: TestContext.Current.CancellationToken);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement content = request.RootElement.GetProperty("input")[0].GetProperty("content");
        Assert.Equal(2, content.GetArrayLength());
        Assert.Equal("Context", content[0].GetProperty("text").GetString());
        Assert.Equal("data:image/png;base64,AQ==", content[1].GetProperty("image_url").GetString());
        Assert.Equal("auto", content[1].GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("audio")]
    [InlineData("too-many")]
    [InlineData("no-input")]
    [InlineData("detail")]
    public async Task CreateDecision_RejectsInvalidImagesBeforeSendingAsync(string scenario)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        DataContent image = new(new byte[] { 1 }, "image/png");
        IEnumerable<DataContent> images = scenario switch
        {
            "empty" => [new DataContent(ReadOnlyMemory<byte>.Empty, "image/png")],
            "audio" => [new DataContent(new byte[] { 1 }, "audio/wav")],
            "too-many" => Enumerable.Repeat(image, 129),
            "no-input" => [],
            _ => [image]
        };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => factory.CreateDecisionAsync<DetailedResult>(new DecisionImageRequest { Images = images.ToArray(), ImageDetail = scenario == "detail" ? (ImageDetail)999 : ImageDetail.Auto }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("probability")]
    [InlineData("choice")]
    [InlineData("duplicate")]
    [InlineData("incomplete-distribution")]
    [InlineData("score")]
    [InlineData("label")]
    public async Task CreateDecision_RejectsInvalidAnswersBeforeConstructingResultAsync(string scenario)
    {
        JsonObject response = CreateResponse();
        JsonArray answers = response["answers"]!.AsArray();
        switch (scenario)
        {
            case "missing": answers.RemoveAt(2); break;
            case "name": answers[0]!["name"] = "Other"; break;
            case "type": answers[0]!["type"] = "choice"; break;
            case "probability": answers[0]!["probability"] = 1.1; break;
            case "choice": answers[1]!["choice"] = "Unknown"; break;
            case "duplicate": answers[1]!["probabilities"]![1]!["value"] = "Low"; break;
            case "incomplete-distribution": answers[1]!["probabilities"]!.AsArray().RemoveAt(1); break;
            case "score": answers[2]!["score"] = 20; break;
            case "label": answers[2]!["probabilities"]![0]!["label"] = "Wrong"; break;
        }
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await Assert.ThrowsAsync<JsonException>(() => factory.CreateDecisionAsync<MustNotConstructResult>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateDecision_RefusalFailsEntireEvaluationBeforeConstructingResultAsync()
    {
        JsonObject response = CreateResponse();
        response["answers"]![2] = new JsonObject { ["name"] = "Severity", ["type"] = "refusal" };
        using RecordingHandler handler = new() { ResponseBody = response.ToJsonString() };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        DecisionRefusalException exception = await Assert.ThrowsAsync<DecisionRefusalException>(() => factory.CreateDecisionAsync<MustNotConstructResult>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Severity", exception.QuestionName);
        Assert.Contains("refused question 'Severity'", exception.Message);
    }

    [Fact]
    public async Task CreateDecision_PropagatesHttpErrorsAsync()
    {
        using RecordingHandler handler = new() { StatusCode = HttpStatusCode.BadRequest };
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        ClientResultException exception = await Assert.ThrowsAsync<ClientResultException>(() => factory.CreateDecisionAsync<DetailedResult>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(400, exception.Status);
    }

    [Fact]
    public async Task CreateDecision_RejectsInvalidResultTypeBeforeSendingAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        OpenAIDecisionFactory factory = new(CreateConnection(client), OpenAIChatModels.Gpt6Luna);
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateDecisionAsync<NoQuestions>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateDecisionAsync<WrongPropertyType>(new DecisionRequest { Input = "Evidence" }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void ServiceCollection_RegistersFactoryAsSingleton()
    {
        ServiceCollection services = new();
        OpenAIConnection connection = new("fake-key");
        services.AddOpenAIDecisionFactory(connection, OpenAIChatModels.Gpt6Luna);
        using ServiceProvider provider = services.BuildServiceProvider();
        OpenAIDecisionFactory factory = provider.GetRequiredService<OpenAIDecisionFactory>();
        Assert.Same(connection, factory.Connection);
        Assert.Same(factory, provider.GetRequiredService<OpenAIDecisionFactory>());
    }

    private static OpenAIConnection CreateConnection(HttpClient client)
    {
        return new("fake-key")
        {
            Endpoint = "https://example.test/custom/v1",
            AdditionalOpenAIClientOptions = options => options.Transport = new HttpClientPipelineTransport(client)
        };
    }

    private static JsonObject CreateResponse()
    {
        return JsonNode.Parse("""
            {"model":"returned-model","answers":[
              {"name":"Damaged","type":"predicate","probability":0.7},
              {"name":"Category","type":"choice","choice":"High","confidence":0.8,
               "probabilities":[{"value":"Low","probability":0.2},{"value":"High","probability":0.8}]},
              {"name":"Severity","type":"score","score":0.7,"confidence":0.6,
               "probabilities":[{"value":0,"label":"Low","probability":0.3},{"value":1,"label":"High","probability":0.7}]}
            ],"usage":{"input_tokens":10,"output_tokens":2,"total_tokens":12}}
            """)!.AsObject();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string ResponseBody { get; init; } = CreateResponse().ToJsonString();
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
        public string? RequestBody { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Method { get; private set; }
        public string? Authorization { get; private set; }
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            RequestUri = request.RequestUri;
            Method = request.Method.Method;
            Authorization = request.Headers.Authorization?.ToString();
            return new(StatusCode) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
        }
    }

    public enum Level
    {
        High = 20,
        [Description("Minor impact")]
        Low = -10
    }

    public class DetailedResult
    {
        [ProbabilityQuestion("Is it damaged?")]
        public Probability? Damaged { get; set; }
        [ChoiceQuestion<Level>("Category?")]
        public Choice<Level>? Category { get; set; }
        [ScoreQuestion<Level>("Severity?")]
        public Score<Level>? Severity { get; set; }
    }

    public class ThresholdResult
    {
        [ProbabilityQuestion("Damaged?", 0.8)]
        public Probability? Damaged { get; set; }
    }

    public class MustNotConstructResult : DetailedResult
    {
        public MustNotConstructResult()
        {
            throw new Exception("An incomplete decision must never construct the result object.");
        }
    }

    public class SimpleResult
    {
        [ProbabilityQuestion("Damaged?", 0.7)] public bool? Damaged { get; set; }
        [ChoiceQuestion<Level>("Category?")] public Level? Category { get; set; }
        [ScoreQuestion<Level>("Severity?")] public decimal? Severity { get; set; }
    }

    public class NumericResult
    {
        [ProbabilityQuestion("Damaged?")] public decimal Damaged { get; set; }
        [ChoiceQuestion<Level>("Category?")] public Level Category { get; set; }
        [ScoreQuestion<Level>("Severity?")] public double Severity { get; set; }
    }

    public class NoQuestions;
    public class WrongPropertyType
    {
        [ProbabilityQuestion("Damaged?")] public string? Damaged { get; set; }
    }
}
