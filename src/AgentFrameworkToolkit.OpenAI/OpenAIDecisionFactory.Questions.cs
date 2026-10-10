using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    private static object BuildQuestion(QuestionDefinition definition)
    {
        Dictionary<string, object> question = new()
        {
            ["type"] = definition.Attribute.Kind,
            ["name"] = definition.Name,
            ["instructions"] = definition.Attribute.Question
        };
        if (definition.Attribute.Kind is "choice" or "score")
        {
            question[definition.Attribute.Kind == "choice" ? "choices" : "levels"] = definition.Names.Select((name, index) =>
                new Dictionary<string, object>
                {
                    [definition.Attribute.Kind == "choice" ? "value" : "label"] = name,
                    ["description"] = definition.GetOptionDescription(index)
                }).ToArray();
        }
        return question;
    }
}
