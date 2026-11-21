using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Web.Evals.Admin.Studio.Schema;

namespace LouisAgent.Web.Evals.Admin.Studio.Compose;

/// <summary>
/// An eval as the compose page edits it (F49.5): its head (name, identity, version, what it proves, its manual), its
/// modes and its checks. It becomes the eval's file in the order the eval folders write theirs, each setting as the
/// kind its schema says and an empty one left out, and an eval's own file reads back into it.
/// </summary>
/// <remarks>
/// A class: the form's inputs bind to it and change it as the person types (ADR 0040). The file is built as a
/// <see cref="JsonObject"/>, since a check's settings are a shape decided by its schema (ADR 0041); the runner, not
/// this class, judges the file (finding R), so a value that doesn't parse as its kind is written as text and the
/// runner's check names it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0058-an-eval-is-a-folder-of-files.md">ADR 0058: an eval is a folder of files</seealso>
public sealed class EvalComposition
{
    /// <summary>How the file is written: indented, as the eval folders keep theirs.</summary>
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The eval's name, in kebab-case: it names its folder.</summary>
    public string Name { get; set; } = "";

    /// <summary>The identity the eval proves.</summary>
    public string Identity { get; set; } = "";

    /// <summary>The eval's version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>What a pass means, in one sentence.</summary>
    public string Proves { get; set; } = "";

    /// <summary>The manual's file beside the eval, or none.</summary>
    public string Manual { get; set; } = "";

    /// <summary>The modes, in order.</summary>
    public List<ModeEntry> Modes { get; } = [];

    /// <summary>The checks, in order.</summary>
    public List<CheckEntry> Checks { get; } = [];

    /// <summary>The eval's file, each check's settings written as their schema says.</summary>
    public string ToJson(IReadOnlyDictionary<string, IReadOnlyList<SchemaField>> fields)
    {
        var file = new JsonObject
        {
            ["schema_version"] = 1,
            ["name"] = Name,
            ["identity"] = Identity,
            ["version"] = Version,
            ["proves"] = Proves
        };
        if (Manual.Length > 0) file["manual"] = Manual;
        file["modes"] = new JsonArray([.. Modes.Select(ModeOf)]);
        file["checks"] = new JsonArray([.. Checks.Select(check => CheckOf(check, fields))]);
        return file.ToJsonString(Indented);
    }

    /// <summary>The form for an eval's own file; a file that isn't one gives an empty form.</summary>
    public static EvalComposition FromJson(string json)
    {
        var eval = new EvalComposition();
        if (Parse(json) is not JsonObject file) return eval;

        eval.Name = Text(file["name"]);
        eval.Identity = Text(file["identity"]);
        eval.Version = int.TryParse(Text(file["version"]), out int version) ? version : 1;
        eval.Proves = Text(file["proves"]);
        eval.Manual = Text(file["manual"]);
        foreach (var mode in (file["modes"] as JsonArray ?? []).OfType<JsonObject>())
            eval.Modes.Add(new ModeEntry
            {
                Name = Text(mode["name"]),
                Role = Text(mode["role"]),
                Toolsets = string.Join(", ", (mode["toolsets"] as JsonArray ?? []).Select(Text))
            });
        foreach (var check in (file["checks"] as JsonArray ?? []).OfType<JsonObject>())
            eval.Checks.Add(EntryOf(check));
        return eval;
    }

    /// <summary>A mode's part of the file.</summary>
    private static JsonObject ModeOf(ModeEntry mode) => new()
    {
        ["name"] = mode.Name,
        ["role"] = mode.Role,
        ["toolsets"] = new JsonArray([.. Words(mode.Toolsets).Select(t => (JsonNode)t)])
    };

    /// <summary>
    /// A check's part of the file: its type, its name if renamed, and the settings that have a value.
    /// </summary>
    private static JsonObject CheckOf(CheckEntry check, IReadOnlyDictionary<string, IReadOnlyList<SchemaField>> fields)
    {
        var part = new JsonObject { ["type"] = check.Type };
        if (check.Name.Length > 0) part["name"] = check.Name;

        var kinds = fields.GetValueOrDefault(check.Type) ?? [];
        var parameters = new JsonObject();
        foreach (var (name, value) in check.Values.Where(v => v.Value.Trim().Length > 0))
            parameters[name] = ValueOf(value.Trim(), kinds.FirstOrDefault(f => f.Name == name)?.Kind);
        if (parameters.Count > 0) part["parameters"] = parameters;
        return part;
    }

    /// <summary>A setting's value as its kind, or as text when it doesn't parse as that kind.</summary>
    /// <remarks>A switch on the kind, one arm each (ADR 0051).</remarks>
    private static JsonNode ValueOf(string value, FieldKind? kind) => kind switch
    {
        FieldKind.Whole when long.TryParse(value, CultureInfo.InvariantCulture, out long whole) => whole,
        FieldKind.Number when double.TryParse(value, CultureInfo.InvariantCulture, out double number) => number,
        FieldKind.YesNo when bool.TryParse(value, out bool yes) => yes,
        FieldKind.List => new JsonArray([.. Words(value).Select(w => (JsonNode)w)]),
        _ => value,
    };

    /// <summary>A check of the file as the form holds it, each setting as the text it is shown as.</summary>
    private static CheckEntry EntryOf(JsonObject check)
    {
        var entry = new CheckEntry { Type = Text(check["type"]), Name = Text(check["name"]) };
        foreach (var (name, value) in check["parameters"] as JsonObject ?? [])
            entry.Values[name] = value is JsonArray list ? string.Join(", ", list.Select(Text)) : Text(value);
        return entry;
    }

    /// <summary>The words of a comma list, trimmed, the empty ones left out.</summary>
    private static string[] Words(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    /// <summary>A node's value as text: a string as itself, anything else as JSON, nothing as empty.</summary>
    private static string Text(JsonNode? node) => node switch
    {
        null => "",
        JsonValue value when value.TryGetValue(out string? text) => text,
        _ => node.ToJsonString(),
    };

    /// <summary>The file as a node, or none when it isn't JSON.</summary>
    private static JsonNode? Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
