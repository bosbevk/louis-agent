namespace LouisAgent.Core.Tools.Scripts;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

/// <summary>
/// Exposes a <see cref="ScriptToolDefinition"/> to the model as a real function with a JSON schema.
/// Arguments are validated against the declared types, then passed to the script as JSON on stdin
/// (and as TOOL_ARG_name environment variables) — never spliced into the code.
/// </summary>
public sealed class ScriptTool : AIFunction
{
    private readonly Func<ScriptToolDefinition, string, IReadOnlyDictionary<string, string>, string> _run;
    private readonly JsonElement _schema;

    public ScriptTool(ScriptToolDefinition definition, bool approved,
        Func<ScriptToolDefinition, string, IReadOnlyDictionary<string, string>, string> run)
    {
        Definition = definition;
        Approved = approved;
        _run = run;
        _schema = BuildSchema(definition);
    }

    public ScriptToolDefinition Definition { get; }

    /// <summary>False while the tool only exists for this session, awaiting the user's /approve.</summary>
    public bool Approved { get; set; }

    public override string Name => Definition.Name;
    public override string Description => $"{Definition.Description} (Agent-built tool{(Approved ? "" : ", pending user approval")}.)";
    public override JsonElement JsonSchema => _schema;

    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        if (!TryBuildInput(arguments, out var json, out var env, out var error))
            return ValueTask.FromResult<object?>(error);

        return new ValueTask<object?>(Task.Run(() => (object?)_run(Definition, json, env), cancellationToken));
    }

    internal bool TryBuildInput(IDictionary<string, object?> arguments, out string json, out Dictionary<string, string> env, out string error)
    {
        var input = new JsonObject();
        env = new Dictionary<string, string>();
        json = "{}";
        error = "";

        var known = Definition.Parameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        if (arguments.Keys.FirstOrDefault(k => !known.ContainsKey(k)) is { } unknown)
        {
            error = $"Error: unknown parameter '{unknown}'. Parameters: {string.Join(", ", known.Keys)}.";
            return false;
        }

        foreach (var parameter in Definition.Parameters)
        {
            arguments.TryGetValue(parameter.Name, out object? raw);
            JsonElement? value = raw switch
            {
                null => null,
                JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
                JsonElement element => element,
                _ => JsonSerializer.SerializeToElement(raw),
            };

            if (value is null)
            {
                if (parameter.Required)
                {
                    error = $"Error: missing required parameter '{parameter.Name}' ({parameter.Type}).";
                    return false;
                }
                continue;
            }

            if (!TryConvert(value.Value, parameter.Type, out JsonNode? node))
            {
                error = $"Error: parameter '{parameter.Name}' must be a {parameter.Type}, got {value.Value.GetRawText()}.";
                return false;
            }

            input[parameter.Name] = node;
            env[$"TOOL_ARG_{parameter.Name}"] = node is JsonValue v && v.TryGetValue(out string? s) ? s : node!.ToJsonString();
        }

        json = input.ToJsonString();
        return true;
    }

    // Models sometimes send numbers/booleans as strings; accept those when they convert cleanly.
    private static bool TryConvert(JsonElement value, string type, out JsonNode? node)
    {
        node = null;
        string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        switch (type)
        {
            case "string":
                node = JsonValue.Create(text ?? value.GetRawText());
                return true;
            case "integer":
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long l)) { node = JsonValue.Create(l); return true; }
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) { node = JsonValue.Create(l); return true; }
                return false;
            case "number":
                if (value.ValueKind == JsonValueKind.Number) { node = JsonValue.Create(value.GetDouble()); return true; }
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) { node = JsonValue.Create(d); return true; }
                return false;
            case "boolean":
                if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) { node = JsonValue.Create(value.GetBoolean()); return true; }
                if (bool.TryParse(text, out bool b)) { node = JsonValue.Create(b); return true; }
                return false;
            default:
                return false;
        }
    }

    private static JsonElement BuildSchema(ScriptToolDefinition definition)
    {
        var properties = new JsonObject();
        foreach (var p in definition.Parameters)
            properties[p.Name] = new JsonObject { ["type"] = p.Type, ["description"] = p.Description };

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(definition.Parameters.Where(p => p.Required).Select(p => (JsonNode)JsonValue.Create(p.Name)).ToArray()),
            ["additionalProperties"] = false,
        };
        return JsonSerializer.SerializeToElement(schema);
    }
}
