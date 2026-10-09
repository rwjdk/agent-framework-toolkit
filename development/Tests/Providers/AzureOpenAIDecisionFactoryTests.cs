using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.Jev;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFrameworkToolkit.Tests.Providers;

public class AzureOpenAIDecisionFactoryTests
{
    [Fact]
    public async Task CreateDecision_MapsMicrosoftWireFormatToSharedResultsAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        IDecisionFactory factory = CreateFactory(client);
        RawCallDetails? raw = null;
        DecisionResponse<SupportDecision> response = await factory.CreateDecisionAsync<SupportDecision>(new DecisionRequest
        {
            Input = "The screen is shattered.",
            RawHttpCallDetails = details => raw = details
        }, TestContext.Current.CancellationToken);

        Assert.True(response.Result.Damaged);
        Assert.Equal(Department.Returns, response.Result.Department!.Value);
        Assert.Equal(0.8, response.Result.Department.Confidence);
        Assert.Equal(0.8, response.Result.Department.Probabilities[Department.Returns]);
        Assert.Equal(0.6, response.Result.Severity!.Value);
        Assert.Equal("Minor impact", response.Result.Severity.LevelDescriptions[Level.Low]);
        Assert.Equal(0.4, response.Result.Severity.Probabilities[Level.Low]);
        Assert.Equal("returned-model", response.Model);
        Assert.Equal(4, response.InputTokenCount);
        Assert.Equal(2, response.OutputTokenCount);
        Assert.Equal(6, response.TotalTokenCount);
        Assert.Equal("Bearer test-key", handler.Authorization);
        Assert.Equal("https://example.services.ai.azure.com/providers/microsoft/v1/systemone", raw!.RequestUrl);
        Assert.Equal(handler.RequestBody, raw.RequestData);
        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("explicit-model", request.RootElement.GetProperty("model").GetString());
        Assert.Equal("The screen is shattered.", request.RootElement.GetProperty("state").GetString());
        Assert.False(request.RootElement.TryGetProperty("input", out _));
        JsonElement questions = request.RootElement.GetProperty("questions");
        Assert.Equal("noul", questions.GetProperty("Damaged").GetProperty("type").GetString());
        Assert.Equal("Returns department", questions.GetProperty("Department").GetProperty("criteria").GetProperty("Returns").GetString());
        Assert.Equal("Minor impact", questions.GetProperty("Severity").GetProperty("criteria")[0].GetString());
        Assert.Equal("Major impact", questions.GetProperty("Severity").GetProperty("criteria")[1].GetString());
    }

    [Theory]
    [InlineData(0.69, false)]
    [InlineData(0.7, true)]
    [InlineData(1, true)]
    public async Task SingleQuestions_MapNoulChoiceAndScoreAsync(double value, bool expected)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        IDecisionFactory factory = CreateFactory(client);
        handler.Response["answers"] = new JsonObject
        {
            ["Value"] = new JsonObject { ["type"] = "noul", ["noul"] = value }
        };
        ProbabilityRequest request = new() { Input = "Evidence", Question = "Damaged?", Threshold = 0.7 };
        Probability probability = await factory.ProbabilityAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(value, probability.Value);
        Assert.Equal(expected, probability.IsTrue);
        Assert.Equal(expected, await factory.IsTrueAsync(request, TestContext.Current.CancellationToken));
        handler.Response["answers"] = new JsonObject { ["Value"] = ChoiceAnswer() };
        Assert.Equal(Department.Returns, await factory.ChooseAsync<Department>(new ChoiceRequest
        {
            Input = "Evidence", Question = "Department?"
        }, TestContext.Current.CancellationToken));
        handler.Response["answers"] = new JsonObject { ["Value"] = ScoreAnswer() };
        Score<Level> score = await factory.ScoreAsync<Level>(new ScoreRequest
        {
            Input = "Evidence", Question = "Severity?"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(0.6, score.Value);
        Assert.Equal("Major impact", score.LevelDescriptions[Level.High]);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("type")]
    [InlineData("probability")]
    [InlineData("choice")]
    [InlineData("score")]
    [InlineData("legend")]
    public async Task CreateDecision_RejectsMalformedAnswersAsync(string invalid)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        JsonObject answers = (JsonObject)handler.Response["answers"]!;
        switch (invalid)
        {
            case "missing": answers.Remove("Damaged"); break;
            case "type": answers["Damaged"]!["type"] = "predicate"; break;
            case "probability": answers["Damaged"]!["noul"] = 1.1; break;
            case "choice": answers["Department"]!["choice"] = "1"; break;
            case "score": answers["Severity"]!["score"] = 2; break;
            case "legend": answers["Severity"]!["legend"]!["2"] = "Unknown"; break;
        }
        await Assert.ThrowsAsync<JsonException>(() => CreateFactory(client).CreateDecisionAsync<SupportDecision>(
            new DecisionRequest { Input = "Evidence" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateDecision_RefusalReturnsNoPartialResultAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        handler.Response["answers"]!["Damaged"] = new JsonObject { ["type"] = "refusal" };
        DecisionRefusalException exception = await Assert.ThrowsAsync<DecisionRefusalException>(() => CreateFactory(client)
            .CreateDecisionAsync<SupportDecision>(new DecisionRequest { Input = "Evidence" }, TestContext.Current.CancellationToken));
        Assert.Equal("Damaged", exception.QuestionName);
    }

    [Fact]
    public async Task ImageOverloadsAndSafetyIdentifier_FailWithoutSendingAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        IDecisionFactory factory = CreateFactory(client);
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.CreateDecisionAsync<SupportDecision>(new DecisionImageRequest { Images = [] }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.ChooseAsync<Department>(new ChoiceImageRequest { Images = [], Question = "Department?" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.ProbabilityAsync(new ProbabilityImageRequest { Images = [], Question = "Damaged?" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.IsTrueAsync(new ProbabilityImageRequest { Images = [], Question = "Damaged?" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.ScoreAsync<Level>(new ScoreImageRequest { Images = [], Question = "Severity?" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => factory.ProbabilityAsync(new ProbabilityRequest
        {
            Input = "Evidence", Question = "Damaged?", SafetyIdentifier = "user"
        }, TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task HttpFailures_ReportRawDetailsAndPreserveStatusAsync()
    {
        using RecordingHandler handler = new() { StatusCode = HttpStatusCode.Unauthorized };
        using HttpClient client = new(handler);
        RawCallDetails? raw = null;
        ClientResultException exception = await Assert.ThrowsAsync<ClientResultException>(() => CreateFactory(client)
            .CreateDecisionAsync<SupportDecision>(new DecisionRequest { Input = "Evidence", RawHttpCallDetails = details => raw = details }, TestContext.Current.CancellationToken));
        Assert.Equal(401, exception.Status);
        Assert.NotNull(raw);
    }

    [Fact]
    public async Task InvalidRequestsAndCancellation_DoNotSendAsync()
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        IDecisionFactory factory = CreateFactory(client);
        Assert.Throws<ArgumentException>(() => new AzureOpenAIDecisionFactory("https://example.services.ai.azure.com", "key", " "));
        Assert.Throws<ArgumentNullException>(() => new AzureOpenAIDecisionFactory((AzureOpenAIConnection)null!, "model"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.ChooseAsync<Department>((ChoiceRequest)null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => factory.ProbabilityAsync(new ProbabilityRequest { Input = " ", Question = "Damaged?" }, TestContext.Current.CancellationToken));
        using CancellationTokenSource cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.CreateDecisionAsync<SupportDecision>(new DecisionRequest { Input = "Evidence" }, cancelled.Token));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void DependencyInjection_ResolvesTheSameFactory()
    {
        ServiceCollection services = new();
        services.AddAzureOpenAIDecisionFactory("https://example.services.ai.azure.com", "key", "model");
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<AzureOpenAIDecisionFactory>(), provider.GetRequiredService<IDecisionFactory>());
        ServiceCollection openAiServices = new();
        openAiServices.AddOpenAIDecisionFactory("key", "model");
        using ServiceProvider openAiProvider = openAiServices.BuildServiceProvider();
        Assert.Same(openAiProvider.GetRequiredService<OpenAIDecisionFactory>(), openAiProvider.GetRequiredService<IDecisionFactory>());
    }

    [Theory]
    [InlineData("https://example.services.ai.azure.com")]
    [InlineData("https://example.services.ai.azure.com/")]
    [InlineData("https://example.services.ai.azure.com/api/projects/test")]
    [InlineData("https://example.services.ai.azure.com/openai/v1")]
    [InlineData("https://example.services.ai.azure.com/providers/microsoft/v1/systemone")]
    [InlineData("https://example.openai.azure.com/openai/v1/")]
    public async Task EndpointVariants_ResolveToResourceDecisionEndpointAsync(string endpoint)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        AzureOpenAIConnection connection = new(endpoint, "test-key")
        {
            AdditionalOpenAIClientOptions = options => options.Transport = new HttpClientPipelineTransport(client)
        };
        IDecisionFactory factory = new AzureOpenAIDecisionFactory(connection, "explicit-model");
        await factory.CreateDecisionAsync<SupportDecision>(new DecisionRequest { Input = "Evidence" }, TestContext.Current.CancellationToken);
        string expectedHost = new Uri(endpoint).Host;
        Assert.Equal($"https://{expectedHost}/providers/microsoft/v1/systemone", handler.RequestUrl);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task KeyedProviders_PreserveCaseDistinctEnumsAndRejectAmbiguousNamesAsync(bool useJev, bool ambiguous)
    {
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        handler.Response["answers"] = new JsonObject
        {
            ["Choice"] = new JsonObject
            {
                ["type"] = "choice", ["choice"] = ambiguous ? "READY" : "ready", ["confidence"] = 0.8,
                ["probabilities"] = new JsonObject { ["Ready"] = 0.2, ["ready"] = 0.8 }
            },
            ["Score"] = new JsonObject
            {
                ["type"] = "score", ["score"] = 0.8, ["confidence"] = 0.8,
                ["probabilities"] = new JsonObject { ["0"] = 0.2, ["1"] = 0.8 },
                ["legend"] = new JsonObject { ["0"] = "First", ["1"] = "Second" }
            }
        };
        IDecisionFactory factory = useJev
            ? new JevDecisionFactory(new JevConnection("test-key") { HttpClientFactory = () => client }, "explicit-model")
            : CreateFactory(client);
        DecisionRequest request = new() { Input = "Evidence" };
        if (ambiguous)
        {
            await Assert.ThrowsAsync<JsonException>(() => factory.CreateDecisionAsync<CaseDistinctDecision>(request, TestContext.Current.CancellationToken));
        }
        else
        {
            DecisionResponse<CaseDistinctDecision> response = await factory.CreateDecisionAsync<CaseDistinctDecision>(request, TestContext.Current.CancellationToken);
            Assert.Equal(CaseDistinctOption.ready, response.Result.Choice!.Value);
            Assert.Equal(0.2, response.Result.Choice.Probabilities[CaseDistinctOption.Ready]);
            Assert.Equal(0.8, response.Result.Choice.Probabilities[CaseDistinctOption.ready]);
            Assert.Equal(0.8, response.Result.Score!.Probabilities[CaseDistinctOption.ready]);
            Assert.Equal("First", response.Result.Score.LevelDescriptions[CaseDistinctOption.Ready]);
            Assert.Equal("Second", response.Result.Score.LevelDescriptions[CaseDistinctOption.ready]);
        }
    }

    public class CaseDistinctDecision
    {
        [ChoiceQuestion<CaseDistinctOption>("Choose")]
        public Choice<CaseDistinctOption>? Choice { get; set; }
        [ScoreQuestion<CaseDistinctOption>("Score")]
        public Score<CaseDistinctOption>? Score { get; set; }
    }

    public enum CaseDistinctOption { Ready, ready }

    private static AzureOpenAIDecisionFactory CreateFactory(HttpClient client)
    {
        return new(new AzureOpenAIConnection("https://example.services.ai.azure.com", "test-key")
        {
            AdditionalOpenAIClientOptions = options => options.Transport = new HttpClientPipelineTransport(client)
        }, "explicit-model");
    }

    private static JsonObject ChoiceAnswer()
    {
        return new()
        {
            ["type"] = "choice",
            ["choice"] = "Returns",
            ["confidence"] = 0.8,
            ["probabilities"] = new JsonObject { ["Support"] = 0.2, ["Returns"] = 0.8 }
        };
    }

    private static JsonObject ScoreAnswer()
    {
        return new()
        {
            ["type"] = "score",
            ["score"] = 0.6,
            ["confidence"] = 0.6,
            ["probabilities"] = new JsonObject { ["0"] = 0.4, ["1"] = 0.6 },
            ["legend"] = new JsonObject { ["0"] = "Minor impact", ["1"] = "Major impact" }
        };
    }

    public class SupportDecision
    {
        [ProbabilityQuestion("Damaged?", threshold: 0.7)]
        public bool Damaged { get; set; }
        [ChoiceQuestion<Department>("Department?")]
        public Choice<Department>? Department { get; set; }
        [ScoreQuestion<Level>("Severity?")]
        public Score<Level>? Severity { get; set; }
    }

    public enum Department
    {
        Support,
        [Description("Returns department")]
        Returns
    }

    public enum Level
    {
        [Description("Major impact")]
        High = 20,
        [Description("Minor impact")]
        Low = -10
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public JsonObject Response { get; } = new()
        {
            ["model"] = "returned-model",
            ["usage"] = new JsonObject { ["input_tokens"] = 4, ["output_tokens"] = 2 },
            ["answers"] = new JsonObject
            {
                ["Damaged"] = new JsonObject { ["type"] = "noul", ["noul"] = 0.7 },
                ["Department"] = ChoiceAnswer(),
                ["Severity"] = ScoreAnswer()
            }
        };
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
        public int RequestCount { get; private set; }
        public string? RequestBody { get; private set; }
        public string? Authorization { get; private set; }
        public string? RequestUrl { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            RequestUrl = request.RequestUri!.AbsoluteUri;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString();
            return new(StatusCode) { Content = new StringContent(Response.ToJsonString(), Encoding.UTF8, "application/json") };
        }
    }
}
