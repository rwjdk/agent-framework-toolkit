using System.ComponentModel;
using System.Reflection;

namespace AgentFrameworkToolkit.Decisions;

internal static class DecisionQuestions
{
    internal sealed record QuestionDefinition(string Name, QuestionAttribute Attribute, Type ValueType, string[] Names, PropertyInfo? Property = null, string[]? Descriptions = null)
    {
        internal string GetOptionDescription(int index) => Descriptions?[index] ?? GetDescription(Attribute.EnumType!, Names[index]);
    }

    private sealed class RuntimeQuestionAttribute(string question, string kind) : QuestionAttribute(question)
    {
        internal override string Kind => kind;
    }

    internal static List<QuestionDefinition> BuildDynamicDefinitions(IList<IQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count == 0)
        {
            throw new ArgumentException("Provide at least one decision question.", nameof(questions));
        }
        List<QuestionDefinition> definitions = [];
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (IQuestion question in questions)
        {
            ArgumentNullException.ThrowIfNull(question);
            ArgumentException.ThrowIfNullOrWhiteSpace(question.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(question.Question);
            if (!ids.Add(question.Id))
            {
                throw new ArgumentException($"Duplicate question ID '{question.Id}'; IDs must be unique ignoring case.", nameof(questions));
            }
            switch (question)
            {
                case ProbabilityQuestion probability:
                    definitions.Add(new(probability.Id, new ProbabilityQuestionAttribute(probability.Question, probability.Threshold), typeof(ProbabilityAnswer), []));
                    break;
                case ChoiceQuestion choice:
                    ArgumentNullException.ThrowIfNull(choice.Choices);
                    if (choice.Choices.Count < 2)
                    {
                        throw new ArgumentException($"Question '{choice.Id}' requires at least two choices.", nameof(questions));
                    }
                    HashSet<string> optionIds = new(StringComparer.OrdinalIgnoreCase);
                    List<string> names = [];
                    List<string> descriptions = [];
                    foreach (ChoiceQuestionOption option in choice.Choices)
                    {
                        ArgumentNullException.ThrowIfNull(option);
                        ArgumentException.ThrowIfNullOrWhiteSpace(option.Id);
                        ArgumentException.ThrowIfNullOrWhiteSpace(option.Description);
                        if (!optionIds.Add(option.Id))
                        {
                            throw new ArgumentException($"Question '{choice.Id}' contains duplicate option ID '{option.Id}'.", nameof(questions));
                        }
                        names.Add(option.Id);
                        descriptions.Add(option.Description);
                    }
                    definitions.Add(new(choice.Id, new RuntimeQuestionAttribute(choice.Question, "choice"), typeof(ChoiceAnswer), names.ToArray(), Descriptions: descriptions.ToArray()));
                    break;
                case ScoreQuestion score:
                    ArgumentNullException.ThrowIfNull(score.Levels);
                    if (score.Levels.Count < 2)
                    {
                        throw new ArgumentException($"Question '{score.Id}' requires at least two levels.", nameof(questions));
                    }
                    string[] levels = score.Levels.ToArray();
                    foreach (string level in levels)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(level);
                    }
                    string[] levelNames = Enumerable.Range(0, levels.Length).Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    definitions.Add(new(score.Id, new RuntimeQuestionAttribute(score.Question, "score"), typeof(ScoreAnswer), levelNames, Descriptions: levels));
                    break;
                default:
                    throw new NotSupportedException($"Question type '{question.GetType().Name}' is unsupported. Use ChoiceQuestion, ScoreQuestion, or ProbabilityQuestion.");
            }
        }
        return definitions;
    }

    internal static List<QuestionDefinition> BuildDefinitions<T>()
    {
        List<QuestionDefinition> definitions = [];
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            QuestionAttribute[] attributes = property.GetCustomAttributes<QuestionAttribute>().ToArray();
            if (attributes.Length == 0)
            {
                continue;
            }
            if (attributes.Length != 1 || property.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0 || !names.Add(property.Name))
            {
                throw new InvalidOperationException($"Question property '{property.Name}' requires one question attribute, a public setter, and a unique name; indexers are unsupported.");
            }
            QuestionAttribute attribute = attributes[0];
            if (string.IsNullOrWhiteSpace(attribute.Question))
            {
                throw new InvalidOperationException($"Question '{property.Name}' has empty instructions.");
            }
            Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Type? enumType = attribute.EnumType;
            string[] enumNames = enumType is null ? [] : GetEnumNames(enumType);
            bool numeric = valueType == typeof(double) || valueType == typeof(decimal);
            bool supported = attribute.Kind switch
            {
                "predicate" => numeric || valueType == typeof(bool) || valueType == typeof(Probability),
                "choice" => valueType == enumType || IsDetails(valueType, typeof(Choice<>), enumType!),
                "score" => numeric || IsDetails(valueType, typeof(Score<>), enumType!),
                _ => false
            };
            if (!supported)
            {
                throw new InvalidOperationException($"Property '{property.Name}' has an unsupported type for {attribute.Kind}.");
            }
            definitions.Add(new(property.Name, attribute, valueType, enumNames, property));
        }
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException($"No decision question properties were found on '{typeof(T).Name}'.");
        }
        return definitions;
    }

    internal static bool IsDetails(Type type, Type genericDefinition, Type enumType)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition && type.GetGenericArguments()[0] == enumType;
    }

    internal static string[] GetEnumNames(Type enumType)
    {
        string[] names = Enum.GetNames(enumType).OrderBy(name => Convert.ToDecimal(Enum.Parse(enumType, name))).ToArray();
        if (names.Length < 2 || Enum.GetValues(enumType).Cast<object>().Distinct().Count() != names.Length)
        {
            throw new InvalidOperationException($"Enum '{enumType.Name}' requires at least two distinct values without aliases.");
        }
        return names;
    }

    internal static string GetDescription(Type enumType, string name)
    {
        return enumType.GetField(name)!.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }
}
