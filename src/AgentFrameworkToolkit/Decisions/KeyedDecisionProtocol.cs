using System.Collections;
using System.Globalization;
using System.Text.Json;
using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.Decisions;

internal static class KeyedDecisionProtocol
{
    private static object[] ConvertAnswers(List<QuestionDefinition> definitions, JsonElement answers)
    {
        if (answers.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Decision answers must be an object keyed by question name.");
        }
        Dictionary<string, JsonElement> indexedAnswers = new(StringComparer.Ordinal);
        foreach (JsonProperty answer in answers.EnumerateObject())
        {
            if (!indexedAnswers.TryAdd(answer.Name, answer.Value))
            {
                throw new JsonException($"Duplicate answer '{answer.Name}'.");
            }
        }
        if (indexedAnswers.Count != definitions.Count)
        {
            throw new JsonException("The Decisions response must answer every question exactly once.");
        }
        object[] values = new object[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            QuestionDefinition definition = definitions[i];
            if (!indexedAnswers.TryGetValue(definition.Name, out JsonElement answer))
            {
                throw new JsonException($"Missing answer '{definition.Name}'.");
            }
            string? kind = answer.GetProperty("type").GetString();
            if (kind == "refusal")
            {
                throw new DecisionRefusalException(definition.Name);
            }
            string expectedKind = definition.Attribute.Kind == "predicate" ? "noul" : definition.Attribute.Kind;
            if (kind != expectedKind)
            {
                throw new JsonException($"Answer '{definition.Name}' has unexpected type '{kind}'.");
            }
            values[i] = kind == "noul" ? ConvertProbability(definition, answer) : ConvertEnumAnswer(definition, answer);
        }
        return values;
    }

    private static object ConvertProbability(QuestionDefinition definition, JsonElement answer)
    {
        JsonElement element = answer.GetProperty("noul");
        double value = ReadProbability(element);
        double threshold = ((ProbabilityQuestionAttribute)definition.Attribute).Threshold;
        if (definition.ValueType == typeof(ProbabilityAnswer))
        {
            return new ProbabilityAnswer(value, threshold);
        }
        if (definition.ValueType == typeof(bool))
        {
            return value >= threshold;
        }
        if (definition.ValueType == typeof(double))
        {
            return value;
        }
        if (definition.ValueType == typeof(decimal))
        {
            return element.GetDecimal();
        }
        return new Probability(value, threshold);
    }

    private static object ConvertEnumAnswer(QuestionDefinition definition, JsonElement answer)
    {
        if (definition.Descriptions is not null)
        {
            return DynamicDecisionAnswers.ReadOptionAnswer(definition, answer, keyed: true);
        }
        Type enumType = definition.Attribute.EnumType!;
        bool choice = definition.Attribute.Kind == "choice";
        double confidence = ReadProbability(answer.GetProperty("confidence"));
        IDictionary probabilities = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(enumType, typeof(double)))!;
        IDictionary descriptions = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(enumType, typeof(string)))!;
        JsonElement entries = answer.GetProperty("probabilities");
        if (entries.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Decision probabilities must be an object.");
        }
        foreach (JsonProperty entry in entries.EnumerateObject())
        {
            string name = choice ? entry.Name : GetLevelName(definition, entry.Name);
            object key = ParseEnumName(definition, name);
            if (probabilities.Contains(key))
            {
                throw new JsonException($"Duplicate probability option '{entry.Name}'.");
            }
            probabilities.Add(key, ReadProbability(entry.Value));
        }
        if (probabilities.Count != definition.Names.Length)
        {
            throw new JsonException($"Answer '{definition.Name}' must include probabilities for every option.");
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
        JsonElement legend = answer.GetProperty("legend");
        if (legend.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Decision score legends must be an object.");
        }
        foreach (JsonProperty entry in legend.EnumerateObject())
        {
            object key = ParseEnumName(definition, GetLevelName(definition, entry.Name));
            if (descriptions.Contains(key))
            {
                throw new JsonException($"Duplicate score legend level '{entry.Name}'.");
            }
            descriptions.Add(key, entry.Value.GetString() ?? throw new JsonException("Missing score level description."));
        }
        if (descriptions.Count != definition.Names.Length)
        {
            throw new JsonException("A score legend must describe every level.");
        }
        return Activator.CreateInstance(definition.ValueType, score, confidence, probabilities, descriptions)!;
    }

    private static string GetLevelName(QuestionDefinition definition, string index)
    {
        if (!int.TryParse(index, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level < 0 || level >= definition.Names.Length)
        {
            throw new JsonException($"Invalid score level '{index}'.");
        }
        return definition.Names[level];
    }

    private static object ParseEnumName(QuestionDefinition definition, string name)
    {
        string? canonicalName = definition.Names.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (canonicalName is null)
        {
            string[] matches = definition.Names.Where(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
            {
                throw new JsonException($"Unknown or ambiguous option '{name}' for question '{definition.Name}'.");
            }
            canonicalName = matches[0];
        }
        return Enum.Parse(definition.Attribute.EnumType!, canonicalName);
    }

    private static double ReadProbability(JsonElement element)
    {
        double value = element.GetDouble();
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new JsonException("Probabilities and confidence must be finite values between zero and one.");
        }
        return value;
    }

    internal static Dictionary<string, object> CreateQuestions(List<QuestionDefinition> definitions)
    {
        return definitions.ToDictionary(definition => definition.Name, BuildQuestion, StringComparer.Ordinal);
    }

    internal static DecisionEvaluation ReadEvaluation(JsonElement root, List<QuestionDefinition> definitions)
    {
        JsonElement usage = root.GetProperty("usage");
        long inputTokens = usage.GetProperty("input_tokens").GetInt64();
        long outputTokens = usage.GetProperty("output_tokens").GetInt64();
        string model = root.GetProperty("model").GetString() ?? throw new JsonException("Missing response model.");
        return new(ConvertAnswers(definitions, root.GetProperty("answers")), model, inputTokens, outputTokens, checked(inputTokens + outputTokens));
    }

    private static object BuildQuestion(QuestionDefinition definition)
    {
        if (definition.Attribute.Kind == "predicate")
        {
            return new { type = "noul", instructions = definition.Attribute.Question };
        }
        if (definition.Attribute.Kind == "choice")
        {
            return new { type = "choice", instructions = definition.Attribute.Question, criteria = definition.Names.Select((name, index) => new KeyValuePair<string, string>(name, definition.GetOptionDescription(index))).ToDictionary() };
        }
        return new { type = "score", instructions = definition.Attribute.Question, criteria = Enumerable.Range(0, definition.Names.Length).Select(definition.GetOptionDescription).ToArray() };
    }
}
