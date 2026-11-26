using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Folders;
using LouisAgent.Core.Skills.Messages.Holes.Parameters;

namespace LouisAgent.Core.Skills.Headers.Agent.Reading;

/// <summary>
/// Reads a skill from its folder, or refuses it saying what to change (F97.1): its <c>SKILL.md</c>, read by the same
/// reader as every prompt and named for its folder; the script its header names, inside the folder, in a language we
/// run; and its <c>parameters.schema.json</c>, each parameter with a type a call's arguments can hold.
/// </summary>
/// <remarks>
/// Guard clauses (ADR 0051), one refusal for each thing that can be wrong, so the agent that wrote the skill is told
/// exactly what to fix, and a skill that would fail later never reaches a chat. Strict, as the header reader is (ADR
/// 0049): nothing is guessed or quietly left out.
/// </remarks>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is backward compatible until something is sold</seealso>
public static class SkillReader
{
    /// <summary>The skill in this folder, as a draft or approved.</summary>
    public static Skill Read(string folder, SkillState state)
    {
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        var file = SkillFileOf(folder, name);
        var (script, language) = ScriptOf(folder, name, file.Header);
        var parameters = ParametersOf(folder, name);

        return new Skill(name, file.Header!.Description, file.Body, folder, script, language, parameters, state);
    }

    /// <summary>The folder's <c>SKILL.md</c>, its header read.</summary>
    private static PromptFile SkillFileOf(string folder, string name)
    {
        string path = Path.Combine(folder, PromptFileName.Skill.Wire());
        if (!File.Exists(path))
        {
            var missing = new SkillFileMissingHoles(folder);
            throw Refused(SkillErrorMessage.SkillFileMissing, missing);
        }

        try
        {
            return PromptHeaders.Split(path, File.ReadAllText(path));
        }
        catch (PromptHeaderException unread)
        {
            var headerUnread = new SkillHeaderUnreadHoles(name, unread.Message);
            throw Refused(SkillErrorMessage.SkillHeaderUnread, headerUnread);
        }
    }

    /// <summary>The script the header names, inside the folder, and its language.</summary>
    private static (string Script, ScriptLanguage Language) ScriptOf(string folder, string name, PromptHeader? header)
    {
        if (header?.Script is not { Length: > 0 } named)
        {
            var notNamed = new ScriptNotNamedHoles(name);
            throw Refused(SkillErrorMessage.ScriptNotNamed, notNamed);
        }

        // The script is run, so it must be the skill's own file: a path that climbs out of the folder is refused.
        string root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
        string script = Path.GetFullPath(Path.Combine(folder, named));
        if (!script.StartsWith(root, StringComparison.Ordinal))
        {
            var outside = new ScriptOutsideFolderHoles(name, named);
            throw Refused(SkillErrorMessage.ScriptOutsideFolder, outside);
        }

        if (!File.Exists(script))
        {
            var missing = new ScriptMissingHoles(name, named);
            throw Refused(SkillErrorMessage.ScriptMissing, missing);
        }

        if (!ScriptExtension.TryLanguageOf(script, out var language))
        {
            var unknown = new ScriptLanguageUnknownHoles(name, Path.GetExtension(script), ScriptExtension.Known);
            throw Refused(SkillErrorMessage.ScriptLanguageUnknown, unknown);
        }

        return (script, language);
    }

    /// <summary>The folder's parameters: the schema as written, each parameter's type, and the required ones.</summary>
    private static SkillParameters ParametersOf(string folder, string name)
    {
        string path = Path.Combine(folder, SkillFiles.ParametersFile);
        if (!File.Exists(path))
        {
            var missing = new SchemaMissingHoles(name);
            throw Refused(SkillErrorMessage.SchemaMissing, missing);
        }

        if (SchemaOf(File.ReadAllText(path)) is not { } schema ||
            !schema.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            var notAnObject = new SchemaNotAnObjectHoles(name);
            throw Refused(SkillErrorMessage.SchemaNotAnObject, notAnObject);
        }

        var types = properties.EnumerateObject().ToDictionary(p => p.Name, p => TypeOf(name, p));
        return new SkillParameters(schema, types, RequiredOf(schema));
    }

    /// <summary>The file's JSON when it is an object of type object; none when it isn't.</summary>
    private static JsonElement? SchemaOf(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (root.TryGetProperty("type", out var type) &&
                (type.ValueKind != JsonValueKind.String || type.GetString() != "object"))
                return null;

            return root.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A parameter's type, which it must name, and which must be one a call's arguments can hold.</summary>
    private static SkillParameterType TypeOf(string name, JsonProperty parameter)
    {
        if (parameter.Value.ValueKind == JsonValueKind.Object &&
            parameter.Value.TryGetProperty("type", out var type) &&
            type.ValueKind == JsonValueKind.String &&
            SettingChoice.TryParse(type.GetString(), out SkillParameterType? known) && known is { } parsed)
            return parsed;

        var untyped = new ParameterTypeUnknownHoles(name, parameter.Name,
            string.Join(", ", SettingChoice.Names<SkillParameterType>()));
        throw Refused(SkillErrorMessage.ParameterTypeUnknown, untyped);
    }

    /// <summary>The parameters a call must give; none when the schema names none.</summary>
    private static IReadOnlyList<string> RequiredOf(JsonElement schema) =>
        schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array
            ? [.. required.EnumerateArray().Where(r => r.ValueKind == JsonValueKind.String).Select(r => r.GetString()!)]
            : [];

    /// <summary>The refusal the agent or the person reads.</summary>
    private static SkillRefusedException Refused(SkillErrorMessage message, object holes) =>
        new(MessageText.Format(message, holes));
}
