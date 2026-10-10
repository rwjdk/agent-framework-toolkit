using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Secrets;
#pragma warning disable OPENAI001
#pragma warning disable AFT999

namespace Sandbox.Providers;


class MyObject
{
    public required string City { get; set; }
    public required int PopulationInMillion { get; set; }
}

public static class AzureOpenAI
{
    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIConnection connection = new AzureOpenAIConnection
        {
            Endpoint = secrets.AzureOpenAiEndpoint,
            ApiKey = secrets.AzureOpenAiKey,
        };


        AzureOpenAIDecisionFactory decisionFactory = new AzureOpenAIDecisionFactory(connection, "Decision-1");

        DynamicDecisionRequest dynamicRequest = new()
        {
            Input = "My card was charged twice. Please refund the duplicate.",
            Questions =
            [
                new ChoiceQuestion
                {
                    Id = "department",
                    Question = "Which team should handle this?",
                    Choices =
                    [
                        new ChoiceQuestionOption { Id = "billing", Description = "Charges, payments, and refunds" },
                        new ChoiceQuestionOption { Id = "technical", Description = "Software defects and outages" }
                    ]
                },
                new ScoreQuestion
                {
                    Id = "urgency",
                    Question = "How urgently should support respond?",
                    Levels = ["Routine", "Soon", "Immediately"]
                },
                new ProbabilityQuestion
                {
                    Id = "refund_requested",
                    Question = "Does the customer request a refund?",
                    Threshold = 0.8
                }
            ]
        };

        DecisionResponse dynamicResponse = await decisionFactory.CreateDecisionAsync(dynamicRequest);
        ChoiceAnswer department = dynamicResponse.GetChoice("department");
        Console.WriteLine($"Department: {department.Value.Id} ({department.Value.Description}), confidence: {department.Confidence:P0}");
        foreach (ChoiceOption option in department.Options)
        {
            Console.WriteLine($"  {option.Id}: {option.Probability:P0}");
        }
        ScoreAnswer urgency = dynamicResponse.GetScore("URGENCY");
        Console.WriteLine($"Urgency: {urgency.Value}, confidence: {urgency.Confidence:P0}");
        foreach (ScoreLevel level in urgency.Levels)
        {
            Console.WriteLine($"  {level.Index}: {level.Description} — {level.Probability:P0}");
        }
        ProbabilityAnswer refund = dynamicResponse.GetProbability("refund_requested");
        Console.WriteLine($"Refund requested: {refund.IsTrue}, probability: {refund.Value:P0}, threshold: {refund.Threshold:P0}");
        Console.WriteLine($"Model: {dynamicResponse.Model}, tokens: {dynamicResponse.TotalTokenCount}");

        Score<Sentiment> scoreAsync = await decisionFactory.ScoreAsync<Sentiment>(new ScoreRequest
        {
            Input = "This device is kinda ok",
            Question = "What does the user think of the product",
        });

        AIAgent a = connection.GetClient().AsAIAgent(new AgentOptions
        {
           Model = "gpt-5.6-luna",
        });

        AgentResponse agentResponse = await a.RunAsync("Hello");


        AzureOpenAIAgentFactory factory = new AzureOpenAIAgentFactory(connection);

        AzureOpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            Model = "gpt-5.6-luna",
            ReasoningEffort = OpenAIReasoningEffort.Low,
            ClientType = ClientType.ResponsesApi,
            
            RawToolCallDetails = Console.WriteLine
        });
        AgentSession session = await agent.CreateSessionAsync();

        AgentResponse response = await agent.RunAsync("What is the capital of France?", session);

        IList<ChatMessage> chatMessages = session.GetMessages();
    }

    enum Sentiment
    {
        Good,
        Neutral,
        Bad
    }

    public class MathResult
    {
        public required int Result { get; set; }
    }
}
