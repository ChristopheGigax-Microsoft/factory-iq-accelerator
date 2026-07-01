using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace FactoryIQ.Agents.Shared.Agents;

public interface IFunctionToolExecutor
{
    IReadOnlyList<ToolDefinition> ToolDefinitions { get; }

    Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default);
}

public interface IPersistentFactoryAgent
{
    string Name { get; }

    Task RegisterAsync(CancellationToken ct = default);

    Task<string> RunAsync(string userQuery, CancellationToken ct = default);

    Task DeleteAsync(CancellationToken ct = default);
}

public sealed record ToolParameter(
    string Name,
    string Description,
    bool Required = true,
    string Type = "string",
    IReadOnlyList<string>? EnumValues = null);

public abstract class FunctionToolExecutorBase : IFunctionToolExecutor
{
    public abstract IReadOnlyList<ToolDefinition> ToolDefinitions { get; }

    public abstract Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default);

    protected static FunctionToolDefinition CreateFunctionTool(
        string name,
        string description,
        params IReadOnlyList<ToolParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return new FunctionToolDefinition(name, description);
        }

        var properties = new Dictionary<string, object?>();
        var required = new List<string>();

        foreach (var parameter in parameters)
        {
            var definition = new Dictionary<string, object?>
            {
                ["type"] = parameter.Type,
                ["description"] = parameter.Description,
            };

            if (parameter.EnumValues is { Count: > 0 })
            {
                definition["enum"] = parameter.EnumValues;
            }

            properties[parameter.Name] = definition;

            if (parameter.Required)
            {
                required.Add(parameter.Name);
            }
        }

        return new FunctionToolDefinition(
            name,
            description,
            BinaryData.FromObjectAsJson(new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required,
                ["additionalProperties"] = false,
            }));
    }

    protected static string GetRequiredString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Missing required string argument '{propertyName}'.");
        }

        return value.GetString()!;
    }
}
