using System.ComponentModel;
using System.Reflection;
using AgentFrameworkToolkit.OpenAI.Decisions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    private sealed record QuestionDefinition(string Name, QuestionAttribute Attribute, Type ValueType, string[] Names, PropertyInfo? Property = null);

    private static List<QuestionDefinition> BuildDefinitions<T>()
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
            if (attribute is ProbabilityQuestionAttribute predicate && predicate.Threshold != 0.5 && valueType != typeof(bool) && valueType != typeof(Probability))
            {
                throw new InvalidOperationException($"A custom threshold on '{property.Name}' requires a Boolean or Probability property.");
            }
            definitions.Add(new(property.Name, attribute, valueType, enumNames, property));
        }
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException($"No OpenAI question properties were found on '{typeof(T).Name}'.");
        }
        return definitions;
    }

    private static bool IsDetails(Type type, Type genericDefinition, Type enumType)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition && type.GetGenericArguments()[0] == enumType;
    }

    private static string[] GetEnumNames(Type enumType)
    {
        string[] names = Enum.GetNames(enumType).OrderBy(name => Convert.ToDecimal(Enum.Parse(enumType, name))).ToArray();
        if (names.Length < 2 || Enum.GetValues(enumType).Cast<object>().Distinct().Count() != names.Length)
        {
            throw new InvalidOperationException($"Enum '{enumType.Name}' requires at least two distinct values without aliases.");
        }
        return names;
    }

    private static object BuildQuestion(QuestionDefinition definition)
    {
        Dictionary<string, object> question = new()
        {
            ["type"] = definition.Attribute.Kind,
            ["name"] = definition.Name,
            ["instructions"] = definition.Attribute.Question
        };
        if (definition.Attribute.EnumType is Type enumType)
        {
            question[definition.Attribute.Kind == "choice" ? "choices" : "levels"] = definition.Names.Select(name =>
                new Dictionary<string, object>
                {
                    [definition.Attribute.Kind == "choice" ? "value" : "label"] = name,
                    ["description"] = GetDescription(enumType, name)
                }).ToArray();
        }
        return question;
    }

    private static string GetDescription(Type enumType, string name)
    {
        return enumType.GetField(name)!.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }
}
