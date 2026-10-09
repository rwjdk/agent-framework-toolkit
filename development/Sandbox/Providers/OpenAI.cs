using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Batching;
using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.Tools;
using AgentSkillsDotNet;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Secrets;
using System.ComponentModel;

#pragma warning disable OPENAI001
#pragma warning disable AFT999

namespace Sandbox.Providers;

public static class OpenAI
{
    public static async Task RunAsync()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();


        OpenAIConnection openAIConnection = new OpenAIConnection
        {
            ApiKey = secrets.OpenAiApiKey,
            DefaultClientType = ClientType.ResponsesApi
        };

        OpenAIAgentFactory agentFactory = new OpenAIAgentFactory(openAIConnection);
        
        OpenAIAgent aiAgent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = "gpt-6-luna"
        });

        OpenAIDecisionFactory decisionFactory = new(openAIConnection, OpenAIChatModels.Gpt6Luna);

        Score<Sentiment> scoreAsync = await decisionFactory.ScoreAsync<Sentiment>(new ScoreRequest
        {
            Input = "This device is kinda ok",
            Question = "What does the user think of the product",
        });
        
        DataContent image = new(
            await File.ReadAllBytesAsync(@"C:\Users\rasmu\Desktop\animals.png"),
            "image/png");

        Probability predicateAsync = await decisionFactory.ProbabilityAsync(new ProbabilityImageRequest
        {
            Images = [image],
            Question = "Is there a goose in this image?"
        });


        DecisionResponse<SupportDecision> outcome = await decisionFactory.CreateDecisionAsync<SupportDecision>(new DecisionRequest
        {
            Input = "The screen is broken",
            RawHttpCallDetails = details =>
            {
                Console.WriteLine(details.RequestUrl);
                Console.WriteLine(details.RequestData);
                Console.WriteLine(details.ResponseData);
            }
        });

        SupportDecision supportDecision = outcome.Result;


        Sentiment chooseAsync = await decisionFactory.ChooseAsync<Sentiment>(new ChoiceRequest
        {
            Input = "This device sucks!",
            Question = "What does the user think of the product",
        });


        bool dd = await decisionFactory.IsTrueAsync(new ProbabilityRequest
        {
            Input = "The screen is nice!",
            Question = "Is there issues with the users device?"
        });

        Console.WriteLine();

        /*
        OpenAIBatchRunner batchRunner = new OpenAIBatchRunner(openAIConnection);

        EmbeddingBatchRun embeddingBatchRun = await batchRunner.RunEmbeddingBatchAsync(new EmbeddingBatchOptions
        {
            Model = "text-embedding-3-small",
            WaitUntilCompleted = true

        }, [
            EmbeddingBatchRequest.Create("Hello"),
            EmbeddingBatchRequest.Create("World"),
        ]);

        IList<EmbeddingBatchRunResult> embeddingBatchRunResults = await embeddingBatchRun.GetResultAsync();
        

        foreach (EmbeddingBatchRunResult result in embeddingBatchRunResults)
        {
            ReadOnlyMemory<float> vector = result.Response!.Vector;
        }
        */

        OpenAIAgentFactory factory = new(openAIConnection);

        AgentSkillsFactory agentSkillsFactory = new();
        AgentSkills agentSkills = agentSkillsFactory.GetAgentSkills("TestData\\AgentSkills");
        IList<AITool> tools = agentSkills.GetAsTools(AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools, new AgentSkillsAsToolsOptions
        {
            IncludeToolForFileContentRead = false
        });

        tools.Add(AIFunctionFactory.Create(PythonRunner.RunPhytonScript, name: "execute_python"));
    
        OpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Instructions = agentSkills.GetInstructions(),
            Model = OpenAIChatModels.Gpt5Nano,
            Tools = tools,
            RawToolCallDetails = details =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(details.ToString());
                Console.ResetColor();
            },
            RawHttpCallDetails = details =>
            {
                Console.WriteLine(details.RequestData);
            },
            StoredOutputEnabled = false
        });

        AgentResponse response = await agent.RunAsync("World3");
        Console.WriteLine(response);
    }

    public class SupportDecision
    {
        [ProbabilityQuestion("Does the customer report a broken product?", threshold: 0.7)]
        public bool IsBroken { get; set; }

        [ChoiceQuestion<Department>("Which department should handle this?")]
        public Choice<Department>? Department { get; set; }

        [ScoreQuestion<Severity>("How severe is the reported impact?")]
        public Score<Severity>? Severity { get; set; }
    }

    public enum Department
    {
        [Description("Invoices, payments, and subscriptions")]
        Billing,
        [Description("Using or troubleshooting the product")]
        Technical,
        Other
    }

    public enum Severity
    {
        [Description("nothing to worry about")]
        Cosmetic,
        Degraded,
        Blocking
    }

    enum Sentiment
    {
        Good,
        Neutral,
        Bad
    }
}
