using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

internal sealed class CodexFunctionTools
{
    private readonly Dictionary<string, AIFunction> _functions = new(StringComparer.Ordinal);
    private readonly IServiceProvider? _services;

    public CodexFunctionTools(IList<AIFunction> functions, IServiceProvider? services)
    {
        _services = services;
        foreach (AIFunction function in functions)
        {
            ArgumentNullException.ThrowIfNull(function);
            ArgumentException.ThrowIfNullOrWhiteSpace(function.Name);
            if (!_functions.TryAdd(function.Name, function))
            {
                throw new ArgumentException("Duplicate Codex tool name: " + function.Name, nameof(functions));
            }
        }
    }

    public JsonArray CreateDefinitions()
    {
        JsonArray definitions = [];
        foreach (AIFunction function in _functions.Values)
        {
            definitions.Add(new JsonObject
            {
                ["type"] = "function",
                ["name"] = function.Name,
                ["description"] = function.Description ?? "",
                ["inputSchema"] = JsonNode.Parse(function.JsonSchema.GetRawText())
            });
        }
        return definitions;
    }

    public bool CanHandle(JsonElement parameters)
    {
        return (!parameters.TryGetProperty("namespace", out JsonElement toolNamespace) ||
                toolNamespace.ValueKind == JsonValueKind.Null || toolNamespace.GetString() == "") &&
            parameters.TryGetProperty("tool", out JsonElement tool) &&
            _functions.ContainsKey(tool.GetString()!);
    }

    public async Task<JsonNode?> InvokeAsync(JsonElement parameters, CodexAgent agent, CodexAgentOptions options,
        IList<ChatMessage> messages, bool isStreaming, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AIFunction function = _functions[parameters.GetProperty("tool").GetString()!];
            JsonElement input = parameters.GetProperty("arguments");
            if (input.ValueKind != JsonValueKind.Object)
            {
                return Result("Tool arguments must be a JSON object.", false);
            }
            AIFunctionArguments arguments = new() { Services = _services };
            foreach (JsonProperty property in input.EnumerateObject())
            {
                arguments[property.Name] = property.Value.Clone();
            }
            FunctionInvocationContext context = new()
            {
                Function = function,
                Arguments = arguments,
                CallContent = new FunctionCallContent(parameters.GetProperty("callId").GetString()!, function.Name, arguments),
                Messages = messages,
                IsStreaming = isStreaming
            };
            Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> invoke =
                static (invocation, token) => invocation.Function.InvokeAsync(invocation.Arguments, token);
            if (options.ToolCallingMiddleware is { } middleware)
            {
                Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next = invoke;
                invoke = (invocation, token) => middleware(agent, invocation, next, token);
            }
            if (options.RawToolCallDetails is not null)
            {
                ToolCallsHandler handler = new(options.RawToolCallDetails);
                Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next = invoke;
                invoke = (invocation, token) => handler.ToolCallingMiddlewareAsync(agent, invocation, next, token);
            }
            object? result = await invoke(context, cancellationToken).ConfigureAwait(false);
            string text = result is string stringResult ? stringResult :
                JsonSerializer.Serialize(result, context.Function.JsonSerializerOptions);
            return Result(text, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result("Tool invocation was cancelled.", false);
        }
        catch (Exception error)
        {
            return Result(error.Message, false);
        }
    }

    public static JsonObject Result(string text, bool success)
    {
        return new JsonObject
        {
            ["success"] = success,
            ["contentItems"] = new JsonArray(new JsonObject { ["type"] = "inputText", ["text"] = text })
        };
    }
}
