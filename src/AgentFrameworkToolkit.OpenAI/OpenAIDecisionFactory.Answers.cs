using System.Collections;
using System.Text.Json;
using AgentFrameworkToolkit.OpenAI.Decisions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    private static object[] ConvertAnswers(List<QuestionDefinition> definitions, JsonElement answers)
    {
        if (answers.ValueKind != JsonValueKind.Array || answers.GetArrayLength() != definitions.Count)
        {
            throw new JsonException("The Decisions response must answer every question exactly once.");
        }
        object[] values = new object[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            QuestionDefinition definition = definitions[i];
            JsonElement answer = answers[i];
            string? name = answer.GetProperty("name").GetString();
            string? kind = answer.GetProperty("type").GetString();
            if (name != definition.Name)
            {
                throw new JsonException($"Expected answer '{definition.Name}' at position {i}, received '{name}'.");
            }
            if (kind == "refusal")
            {
                throw new DecisionRefusalException(definition.Name);
            }
            if (kind != definition.Attribute.Kind)
            {
                throw new JsonException($"Answer '{name}' has unexpected type '{kind}'.");
            }
            values[i] = kind switch
            {
                "predicate" => ConvertPredicate(definition, answer),
                "choice" or "score" => ConvertEnumAnswer(definition, answer),
                _ => throw new JsonException($"Unsupported answer type '{kind}'.")
            };
        }
        return values;
    }

    private static object ConvertPredicate(QuestionDefinition definition, JsonElement answer)
    {
        double probability = ReadProbability(answer.GetProperty("probability"));
        if (definition.ValueType == typeof(bool))
        {
            return probability >= ((ProbabilityQuestionAttribute)definition.Attribute).Threshold;
        }
        if (definition.ValueType == typeof(decimal))
        {
            return answer.GetProperty("probability").GetDecimal();
        }
        if (definition.ValueType == typeof(double))
        {
            return probability;
        }
        return new Probability(probability, ((ProbabilityQuestionAttribute)definition.Attribute).Threshold);
    }

    private static object ConvertEnumAnswer(QuestionDefinition definition, JsonElement answer)
    {
        Type enumType = definition.Attribute.EnumType!;
        bool choice = definition.Attribute.Kind == "choice";
        double confidence = ReadProbability(answer.GetProperty("confidence"));
        IDictionary probabilities = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(enumType, typeof(double)))!;
        IDictionary levelDescriptions = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(enumType, typeof(string)))!;
        JsonElement entries = answer.GetProperty("probabilities");
        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() != definition.Names.Length)
        {
                throw new JsonException($"Answer '{definition.Name}' must include probabilities for every option.");
        }
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            string name;
            if (choice)
            {
                name = entry.GetProperty("value").GetString() ?? throw new JsonException("Missing choice value.");
            }
            else
            {
                int index = entry.GetProperty("value").GetInt32();
                if (index < 0 || index >= definition.Names.Length)
                {
                    throw new JsonException("Invalid score level index.");
                }
                name = definition.Names[index];
                if (entry.GetProperty("label").GetString() != name)
                {
                    throw new JsonException("Score level label does not match its index.");
                }
            }
            object key = ParseEnumName(definition, name);
            if (probabilities.Contains(key))
            {
                throw new JsonException($"Duplicate probability option '{name}'.");
            }
            probabilities.Add(key, ReadProbability(entry.GetProperty("probability")));
            levelDescriptions.Add(key, GetDescription(enumType, name));
        }
        if (choice)
        {
            object selected = ParseEnumName(definition, answer.GetProperty("choice").GetString() ?? throw new JsonException("Missing choice."));
            return definition.ValueType == enumType ? selected : Activator.CreateInstance(definition.ValueType, selected, confidence, probabilities)!;
        }
        JsonElement scoreElement = answer.GetProperty("score");
        double score = scoreElement.GetDouble();
        if (!double.IsFinite(score) || score < 0 || score > definition.Names.Length - 1)
        {
            throw new JsonException("Score is outside the defined level range.");
        }
        if (definition.ValueType == typeof(double))
        {
            return score;
        }
        if (definition.ValueType == typeof(decimal))
        {
            return scoreElement.GetDecimal();
        }
        return Activator.CreateInstance(definition.ValueType, score, confidence, probabilities, levelDescriptions)!;
    }

    private static object ParseEnumName(QuestionDefinition definition, string name)
    {
        if (!definition.Names.Contains(name, StringComparer.Ordinal))
        {
            throw new JsonException($"Unknown option '{name}' for question '{definition.Name}'.");
        }
        return Enum.Parse(definition.Attribute.EnumType!, name);
    }

    private static double ReadProbability(JsonElement element)
    {
        double probability = element.GetDouble();
        if (!double.IsFinite(probability) || probability is < 0 or > 1)
        {
            throw new JsonException("Probabilities and confidence must be finite values between zero and one.");
        }
        return probability;
    }
}
