using System.Globalization;
using System.Text.Json;
using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.Decisions;

internal static class DynamicDecisionAnswers
{
    internal static object ReadOptionAnswer(QuestionDefinition definition, JsonElement answer, bool keyed)
    {
        bool choice = definition.Attribute.Kind == "choice";
        double confidence = ReadProbability(answer.GetProperty("confidence"));
        JsonElement entries = answer.GetProperty("probabilities");
        Dictionary<int, double> probabilities = [];
        if (keyed)
        {
            if (entries.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Decision probabilities must be an object.");
            }
            foreach (JsonProperty entry in entries.EnumerateObject())
            {
                AddProbability(GetOptionIndex(definition, entry.Name, choice), ReadProbability(entry.Value));
            }
        }
        else
        {
            if (entries.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Decision probabilities must be an array.");
            }
            foreach (JsonElement entry in entries.EnumerateArray())
            {
                int index = choice
                    ? GetOptionIndex(definition, entry.GetProperty("value").GetString() ?? throw new JsonException("Missing choice value."), true)
                    : entry.GetProperty("value").GetInt32();
                if (index < 0 || index >= definition.Names.Length)
                {
                    throw new JsonException("Invalid score level index.");
                }
                if (!choice && entry.GetProperty("label").GetString() != definition.Names[index])
                {
                    throw new JsonException("Score level label does not match its index.");
                }
                AddProbability(index, ReadProbability(entry.GetProperty("probability")));
            }
        }
        if (probabilities.Count != definition.Names.Length)
        {
            throw new JsonException($"Answer '{definition.Name}' must include probabilities for every option.");
        }
        if (choice)
        {
            int selected = GetOptionIndex(definition, answer.GetProperty("choice").GetString() ?? throw new JsonException("Missing choice."), true);
            ChoiceOption[] options = definition.Names.Select((name, index) => new ChoiceOption(name, definition.GetOptionDescription(index), probabilities[index])).ToArray();
            return new ChoiceAnswer(options[selected], confidence, options);
        }
        double score = answer.GetProperty("score").GetDouble();
        if (!double.IsFinite(score) || score < 0 || score > definition.Names.Length - 1)
        {
            throw new JsonException("Score is outside the defined level range.");
        }
        string[] descriptions = definition.Descriptions!.ToArray();
        if (keyed)
        {
            JsonElement legend = answer.GetProperty("legend");
            if (legend.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Decision score legends must be an object.");
            }
            HashSet<int> indices = [];
            foreach (JsonProperty entry in legend.EnumerateObject())
            {
                int index = GetOptionIndex(definition, entry.Name, false);
                if (!indices.Add(index))
                {
                    throw new JsonException($"Duplicate score legend level '{entry.Name}'.");
                }
                descriptions[index] = entry.Value.GetString() ?? throw new JsonException("Missing score level description.");
            }
            if (indices.Count != definition.Names.Length)
            {
                throw new JsonException("A score legend must describe every level.");
            }
        }
        ScoreLevel[] levels = descriptions.Select((description, index) => new ScoreLevel(index, description, probabilities[index])).ToArray();
        return new ScoreAnswer(score, confidence, levels);

        void AddProbability(int index, double probability)
        {
            if (!probabilities.TryAdd(index, probability))
            {
                throw new JsonException($"Duplicate probability option '{definition.Names[index]}'.");
            }
        }
    }

    private static int GetOptionIndex(QuestionDefinition definition, string name, bool choice)
    {
        if (choice)
        {
            int index = Array.FindIndex(definition.Names, candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                return index;
            }
        }
        else if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < definition.Names.Length)
        {
            return index;
        }
        throw new JsonException($"Unknown option '{name}' for question '{definition.Name}'.");
    }

    internal static double ReadProbability(JsonElement element)
    {
        double probability = element.GetDouble();
        if (!double.IsFinite(probability) || probability is < 0 or > 1)
        {
            throw new JsonException("Probabilities and confidence must be finite values between zero and one.");
        }
        return probability;
    }
}
