using AgentFrameworkToolkit.Anthropic;
using AgentFrameworkToolkit.Google;
using AgentFrameworkToolkit.OpenAI;
using Anthropic.Models.Messages;
using Google.GenAI.Types;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Responses;
using OpenAIAgentOptions = AgentFrameworkToolkit.OpenAI.AgentOptions;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.Tests;

public class ProviderRawRepresentationFactoryTests
{
    [Theory]
    [InlineData(OpenAIReasoningSummaryVerbosity.Auto, false)]
    [InlineData(OpenAIReasoningSummaryVerbosity.Concise, false)]
    [InlineData(OpenAIReasoningSummaryVerbosity.Detailed, false)]
    [InlineData(OpenAIReasoningSummaryVerbosity.Auto, true)]
    [InlineData(OpenAIReasoningSummaryVerbosity.Concise, true)]
    [InlineData(OpenAIReasoningSummaryVerbosity.Detailed, true)]
    public void OpenAIResponsesApiFactoryPreservesSummaryWithoutEffort(OpenAIReasoningSummaryVerbosity summaryVerbosity, bool includeOtherOptions)
    {
        foreach (ClientType? clientType in new ClientType?[] { ClientType.ResponsesApi, null })
        {
            ChatClientAgentOptions agentOptions = OpenAIAgentFactory.CreateChatClientAgentOptions(new OpenAIAgentOptions
            {
                Model = OpenAIChatModels.Gpt5,
                ClientType = clientType,
                ReasoningSummaryVerbosity = summaryVerbosity,
                StoredOutputEnabled = includeOtherOptions ? true : null,
                ServiceTier = includeOtherOptions ? OpenAIServiceTier.Priority : null
            }, clientType.HasValue ? ClientType.ChatClient : ClientType.ResponsesApi);

            Func<IChatClient, object?> factory = GetRawRepresentationFactory(agentOptions);
            CreateResponseOptions first = Assert.IsType<CreateResponseOptions>(factory(null!));
            CreateResponseOptions second = Assert.IsType<CreateResponseOptions>(factory(null!));

            Assert.NotSame(first, second);
            Assert.NotSame(first.ReasoningOptions, second.ReasoningOptions);
            ResponseReasoningSummaryVerbosity expectedSummary = summaryVerbosity switch
            {
                OpenAIReasoningSummaryVerbosity.Auto => ResponseReasoningSummaryVerbosity.Auto,
                OpenAIReasoningSummaryVerbosity.Concise => ResponseReasoningSummaryVerbosity.Concise,
                OpenAIReasoningSummaryVerbosity.Detailed => ResponseReasoningSummaryVerbosity.Detailed,
                _ => throw new ArgumentOutOfRangeException(nameof(summaryVerbosity))
            };

            foreach (CreateResponseOptions responseOptions in new[] { first, second })
            {
                Assert.NotNull(responseOptions.ReasoningOptions);
                Assert.Null(responseOptions.ReasoningOptions.ReasoningEffortLevel);
                Assert.Equal(expectedSummary, responseOptions.ReasoningOptions.ReasoningSummaryVerbosity);
                Assert.Equal(includeOtherOptions ? (bool?)true : null, responseOptions.StoredOutputEnabled);
                Assert.Equal(includeOtherOptions ? new ResponseServiceTier("priority") : (ResponseServiceTier?)null, responseOptions.ServiceTier);
            }
        }
    }

    [Fact]
    public void OpenAIResponsesApiFactoryOmitsUnsetReasoningOptions()
    {
        ChatClientAgentOptions agentOptions = OpenAIAgentFactory.CreateChatClientAgentOptions(new OpenAIAgentOptions
        {
            Model = OpenAIChatModels.Gpt5,
            ClientType = ClientType.ResponsesApi,
            StoredOutputEnabled = true
        }, ClientType.ChatClient);

        CreateResponseOptions responseOptions = Assert.IsType<CreateResponseOptions>(GetRawRepresentationFactory(agentOptions)(null!));

        Assert.True(responseOptions.StoredOutputEnabled);
        Assert.Null(responseOptions.ReasoningOptions);
    }

    [Fact]
    public void OpenAIResponsesApiFactoryReturnsIndependentConfiguredOptions()
    {
        ChatClientAgentOptions agentOptions = OpenAIAgentFactory.CreateChatClientAgentOptions(new OpenAIAgentOptions
        {
            Model = OpenAIChatModels.Gpt5,
            ClientType = ClientType.ResponsesApi,
            ReasoningEffort = OpenAIReasoningEffort.High,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed,
            StoredOutputEnabled = true,
            ServiceTier = OpenAIServiceTier.Priority
        }, ClientType.ChatClient);

        Func<IChatClient, object?> factory = GetRawRepresentationFactory(agentOptions);
        CreateResponseOptions first = Assert.IsType<CreateResponseOptions>(factory(null!));
        CreateResponseOptions second = Assert.IsType<CreateResponseOptions>(factory(null!));

        Assert.NotSame(first, second);
        Assert.NotSame(first.ReasoningOptions, second.ReasoningOptions);

        first.PreviousResponseId = "response-a";
        first.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem("call-a", "output-a"));

        Assert.Null(second.PreviousResponseId);
        Assert.Empty(second.InputItems);
        AssertResponsesApiConfiguration(first);
        AssertResponsesApiConfiguration(second);
    }

    [Fact]
    public void OpenAIChatClientFactoryReturnsIndependentConfiguredOptions()
    {
        ChatClientAgentOptions agentOptions = OpenAIAgentFactory.CreateChatClientAgentOptions(new OpenAIAgentOptions
        {
            Model = OpenAIChatModels.Gpt5,
            ClientType = ClientType.ChatClient,
            ReasoningEffort = OpenAIReasoningEffort.High,
            StoredOutputEnabled = true,
            ServiceTier = OpenAIServiceTier.Priority
        }, ClientType.ResponsesApi);

        Func<IChatClient, object?> factory = GetRawRepresentationFactory(agentOptions);
        ChatCompletionOptions first = Assert.IsType<ChatCompletionOptions>(factory(null!));
        ChatCompletionOptions second = Assert.IsType<ChatCompletionOptions>(factory(null!));

        Assert.NotSame(first, second);
        first.StoredOutputEnabled = false;

        Assert.False(first.StoredOutputEnabled);
        AssertChatClientConfiguration(second);

        ChatCompletionOptions third = Assert.IsType<ChatCompletionOptions>(factory(null!));
        AssertChatClientConfiguration(third);
    }

    [Fact]
    public void AnthropicFactoryReturnsIndependentNestedOptions()
    {
        ChatClientAgentOptions agentOptions = AnthropicAgentFactory.CreateChatClientAgentOptions(new AnthropicAgentOptions
        {
            Model = "claude-test",
            MaxOutputTokens = 1024,
            BudgetTokens = 256,
            Effort = Effort.High,
            ServiceTier = global::Anthropic.Models.Messages.ServiceTier.Auto
        });

        Func<IChatClient, object?> factory = GetRawRepresentationFactory(agentOptions);
        MessageCreateParams first = Assert.IsType<MessageCreateParams>(factory(null!));
        MessageCreateParams second = Assert.IsType<MessageCreateParams>(factory(null!));

        Assert.NotSame(first, second);
        Assert.NotSame(first.Messages, second.Messages);
        Assert.NotSame(first.Thinking, second.Thinking);
        Assert.NotSame(first.OutputConfig, second.OutputConfig);
        Assert.Empty(first.Messages);
        Assert.Empty(second.Messages);
    }

    [Fact]
    public void GoogleFactoryReturnsIndependentNestedOptions()
    {
        ChatClientAgentOptions agentOptions = GoogleAgentFactory.CreateChatClientAgentOptions(new GoogleAgentOptions
        {
            Model = "gemini-test",
            ThinkingBudget = 256,
            IncludeThoughts = true,
            ServiceTier = global::Google.GenAI.Types.ServiceTier.Priority
        });

        Func<IChatClient, object?> factory = GetRawRepresentationFactory(agentOptions);
        GenerateContentConfig first = Assert.IsType<GenerateContentConfig>(factory(null!));
        GenerateContentConfig second = Assert.IsType<GenerateContentConfig>(factory(null!));

        Assert.NotSame(first, second);
        Assert.NotSame(first.ThinkingConfig, second.ThinkingConfig);
        Assert.Equal(256, first.ThinkingConfig!.ThinkingBudget);
        Assert.Equal(256, second.ThinkingConfig!.ThinkingBudget);
        Assert.True(first.ThinkingConfig.IncludeThoughts);
        Assert.True(second.ThinkingConfig.IncludeThoughts);
    }

    private static Func<IChatClient, object?> GetRawRepresentationFactory(ChatClientAgentOptions agentOptions)
    {
        ChatOptions chatOptions = Assert.IsType<ChatOptions>(agentOptions.ChatOptions);
        return Assert.IsType<Func<IChatClient, object?>>(chatOptions.RawRepresentationFactory);
    }

    private static void AssertResponsesApiConfiguration(CreateResponseOptions options)
    {
        Assert.True(options.StoredOutputEnabled);
        Assert.Equal(new ResponseReasoningEffortLevel("high"), options.ReasoningOptions!.ReasoningEffortLevel);
        Assert.Equal(ResponseReasoningSummaryVerbosity.Detailed, options.ReasoningOptions.ReasoningSummaryVerbosity);
        Assert.Equal(new ResponseServiceTier("priority"), options.ServiceTier);
    }

    private static void AssertChatClientConfiguration(ChatCompletionOptions options)
    {
        Assert.True(options.StoredOutputEnabled);
        Assert.Equal(new ChatReasoningEffortLevel("high"), options.ReasoningEffortLevel);
        Assert.Equal(new ChatServiceTier("priority"), options.ServiceTier);
    }
}
