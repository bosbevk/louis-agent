namespace louis_agent.core.tools;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

/// <summary>A typed parameter of an agent-built tool.</summary>
public sealed record ScriptToolParameter(string Name, string Type, bool Required, string Description);

/// <summary>
/// An agent-built tool defined in a *.tool.md file: name, description, typed parameters and a script implementation
/// (python, powershell or bash) that receives its arguments as a JSON object on stdin.
/// </summary>
public sealed partial record ScriptToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<ScriptToolParameter> Parameters,
    string Language,
    string Code,
    int TimeoutSeconds)
{
    public const int DefaultTimeoutSeconds = 60;
    private static readonly string[] SupportedTypes = ["string", "integer", "number", "boolean"];

    /// <summary>Parses a tool definition, returning every problem found so the author can fix them in one go.</summary>
    public static (ScriptToolDefinition? Definition, List<string> Errors) Parse(string markdown)
    {
        var errors = new List<string>();
        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');

        string? name = null, description = null, language = null, code = null;
        int timeout = DefaultTimeoutSeconds;
        var parameters = new List<ScriptToolParameter>();
        bool inParameters = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd();
            string trimmed = line.Trim();

            if (ToolHeader().Match(trimmed) is { Success: true } header)
            {
                name = header.Groups["name"].Value.Trim();
                inParameters = false;
                continue;
            }

            if (Field().Match(trimmed) is { Success: true } field && !line.StartsWith("  "))
            {
                inParameters = false;
                string key = field.Groups["key"].Value.ToLowerInvariant();
                string value = field.Groups["value"].Value.Trim();
                switch (key)
                {
                    case "description":
                        description = value;
                        break;
                    case "timeout":
                        if (!int.TryParse(value, out timeout) || timeout is < 1 or > 600)
                            errors.Add($"Timeout must be a number of seconds between 1 and 600, got '{value}'.");
                        break;
                    case "parameters":
                        inParameters = true;
                        break;
                    case "implementation":
                        (language, code, i) = ReadCodeBlock(lines, i + 1, errors);
                        break;
                }
                continue;
            }

            if (inParameters && trimmed.StartsWith('-'))
            {
                if (ParameterLine().Match(trimmed) is { Success: true } p)
                {
                    string type = p.Groups["type"].Value.ToLowerInvariant();
                    if (!SupportedTypes.Contains(type))
                        errors.Add($"Parameter '{p.Groups["name"].Value}' has unsupported type '{type}' (use {string.Join(", ", SupportedTypes)}).");
                    parameters.Add(new ScriptToolParameter(p.Groups["name"].Value, type,
                        p.Groups["req"].Value.Equals("required", StringComparison.OrdinalIgnoreCase), p.Groups["desc"].Value.Trim()));
                }
                else
                {
                    errors.Add($"Cannot parse parameter line: '{trimmed}'. Expected: - `name` (string|integer|number|boolean, required|optional): description");
                }
            }
        }

        if (name is null) errors.Add("Missing '# Tool: <PascalCaseName>' header.");
        else if (!ToolName().IsMatch(name)) errors.Add($"Tool name '{name}' must be PascalCase letters/digits (3-64 chars), e.g. 'CsvSummary'.");
        if (string.IsNullOrWhiteSpace(description)) errors.Add("Missing '- Description:' line.");
        if (code is null && !errors.Any(e => e.Contains("Implementation"))) errors.Add("Missing '- Implementation:' line followed by a fenced code block.");
        foreach (var duplicate in parameters.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"Parameter '{duplicate.Key}' is declared more than once.");

        return errors.Count > 0
            ? (null, errors)
            : (new ScriptToolDefinition(name!, description!, parameters, language!, code!, timeout), errors);
    }

    private static (string? Language, string? Code, int LastLine) ReadCodeBlock(string[] lines, int start, List<string> errors)
    {
        int i = start;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
        if (i >= lines.Length || !lines[i].TrimStart().StartsWith("```"))
        {
            errors.Add("'- Implementation:' must be followed by a fenced code block (```python, ```powershell or ```bash).");
            return (null, null, i);
        }

        string fence = lines[i].Trim()[3..].Trim().ToLowerInvariant();
        string language = MarkdownSkillLoader.NormalizeLanguage(fence);
        if (fence.Length > 0 && language == "bash" && fence is not ("bash" or "sh" or "shell"))
            errors.Add($"Unsupported implementation language '{fence}' (use python, powershell or bash).");

        var body = new List<string>();
        for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```"); i++) body.Add(lines[i]);
        if (i >= lines.Length) errors.Add("The implementation code block is not closed with ```.");

        string code = string.Join('\n', body).Trim();
        if (code.Length == 0) errors.Add("The implementation code block is empty.");
        return (language, code, i);
    }

    [GeneratedRegex(@"^#\s*Tool:\s*(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ToolHeader();

    [GeneratedRegex(@"^-\s*(?:\*\*)?(?<key>Description|Timeout|Parameters|Implementation)(?:\*\*)?\s*:\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex Field();

    [GeneratedRegex(@"^-\s*`(?<name>[A-Za-z_][A-Za-z0-9_]*)`\s*\(\s*(?<type>[A-Za-z]+)\s*,\s*(?<req>required|optional)\s*\)\s*:?\s*(?<desc>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParameterLine();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]{2,63}$")]
    private static partial Regex ToolName();
}

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
