using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Fields;
using LouisAgent.Core.Skills.Messages.Holes.Reading;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// Reads a prompt's front matter (CL27): a YAML block between two <c>---</c> lines at the top of the file, with the
/// Agent Skills spec's <c>name</c> and <c>description</c> and ours under <c>metadata</c> (<c>level</c>,
/// <c>toolsets</c>), and gives it apart from the body the model reads:
/// <code>
/// ---
/// name: acme-helper
/// description: Acme's helper. What it does, and when it is loaded.
/// metadata:
///   level: specialist
///   toolsets: files?
/// ---
/// </code>
/// A file that doesn't open with the fence has no header and is read whole.
/// </summary>
/// <remarks>
/// A strict reader (ADR 0049, no lenient reader): the YAML is read as YamlDotNet's node model, every key checked
/// against the few it takes, so a misspelled <c>toolset</c> refuses the file instead of quietly loading nothing. Guard
/// clauses (ADR 0051), each refusal naming the file. The metadata values are text, as the spec's map of strings is;
/// the toolsets a comma list, as F33's line was.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is backward compatible until something is sold</seealso>
public static class PromptHeaders
{
    /// <summary>The longest description the spec takes.</summary>
    public const int MaxDescription = 1024;

    private const string Fence = "---";

    private static readonly string[] Keys = ["name", "description", "metadata"],
        MetadataKeys = ["level", "toolsets", "load"];

    /// <summary>The file's header, when it has one, and its body.</summary>
    public static PromptFile Split(string path, string text)
    {
        string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string open = Fence + newline, close = newline + Fence + newline;
        if (!text.StartsWith(open, StringComparison.Ordinal)) return new PromptFile(path, null, text);

        int end = text.IndexOf(close, open.Length - newline.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            var unclosed = new NotClosedHoles(path);
            throw Refuse(PromptHeaderMessage.NotClosed, unclosed);
        }

        string yaml = end < open.Length ? "" : text[open.Length..end];
        return new PromptFile(path, Read(path, yaml), text[(end + close.Length)..]);
    }

    /// <summary>The header in the YAML, every key checked.</summary>
    private static PromptHeader Read(string path, string yaml)
    {
        var root = RootOf(path, yaml);
        Known(path, root, Keys);
        string expected = NameFor(path);
        string? name = Text(path, root, "name");
        if (name != expected)
        {
            var misnamed = new NameNotTheFilesHoles(path, name ?? "", expected);
            throw Refuse(PromptHeaderMessage.NameNotTheFiles, misnamed);
        }

        if (Text(path, root, "description") is not { Length: > 0 and <= MaxDescription } description)
        {
            var undescribed = new DescriptionMissingHoles(path, MaxDescription);
            throw Refuse(PromptHeaderMessage.DescriptionMissing, undescribed);
        }

        var metadata = Child(root, "metadata") as YamlMappingNode;
        if (metadata is not null) Known(path, metadata, MetadataKeys);
        return new PromptHeader(name, description, LevelOf(path, metadata), ToolsetsOf(path, metadata),
            LoadOf(path, metadata));
    }

    /// <summary>
    /// The name a prompt's header must give: a skill's folder's for its <c>SKILL.md</c> (CL68.1, the shape Claude
    /// Code's skills have), every other prompt's own file's.
    /// </summary>
    private static string NameFor(string path) =>
        Path.GetFileName(path) == SkillFiles.SkillFile
            ? Path.GetFileName(Path.GetDirectoryName(path))!
            : Path.GetFileNameWithoutExtension(path);

    /// <summary>The YAML's top mapping; an empty header is an empty one.</summary>
    private static YamlMappingNode RootOf(string path, string yaml)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException ex)
        {
            var unread = new NotYamlHoles(path, ex.Message);
            throw Refuse(PromptHeaderMessage.NotYaml, unread);
        }

        return stream.Documents.FirstOrDefault()?.RootNode as YamlMappingNode ?? new YamlMappingNode();
    }

    /// <summary>Refuses a key the mapping may not have.</summary>
    private static void Known(string path, YamlMappingNode mapping, string[] keys)
    {
        var key = mapping.Children.Keys.OfType<YamlScalarNode>().FirstOrDefault(k => !keys.Contains(k.Value));
        if (key is null) return;

        var unknown = new KeyUnknownHoles(path, key.Value ?? "");
        throw Refuse(PromptHeaderMessage.KeyUnknown, unknown);
    }

    /// <summary>The level a role or mode runs at, by its wire name; none for a guide.</summary>
    private static RoleLevel? LevelOf(string path, YamlMappingNode? metadata)
    {
        if (metadata is null || Text(path, metadata, "level") is not { } level) return null;
        if (SettingChoice.TryParse(level, out RoleLevel? parsed)) return parsed;

        var unknown = new LevelUnknownHoles(path, level, string.Join(", ", SettingChoice.Names<RoleLevel>()));
        throw Refuse(PromptHeaderMessage.LevelUnknown, unknown);
    }

    /// <summary>How a skill loads (F129), by its wire name; none for a prompt that isn't a skill.</summary>
    private static SkillLoad? LoadOf(string path, YamlMappingNode? metadata)
    {
        if (metadata is null || Text(path, metadata, "load") is not { } load) return null;
        if (SettingChoice.TryParse(load, out SkillLoad? parsed)) return parsed;

        var unknown = new LoadUnknownHoles(path, load, string.Join(", ", SettingChoice.Names<SkillLoad>()));
        throw Refuse(PromptHeaderMessage.LoadUnknown, unknown);
    }

    /// <summary>The toolsets the prompt always loads, a comma list; none when it names none.</summary>
    private static IReadOnlyList<string> ToolsetsOf(string path, YamlMappingNode? metadata) =>
        metadata is not null && Text(path, metadata, "toolsets") is { } toolsets
            ? toolsets.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>A key's text, or none when the mapping lacks it; a list or a mapping there refuses.</summary>
    private static string? Text(string path, YamlMappingNode mapping, string key)
    {
        var node = Child(mapping, key);
        if (node is null) return null;
        if (node is YamlScalarNode scalar) return scalar.Value;

        var notText = new ValueNotTextHoles(path, key);
        throw Refuse(PromptHeaderMessage.ValueNotText, notText);
    }

    /// <summary>A key's node, or none.</summary>
    private static YamlNode? Child(YamlMappingNode mapping, string key) =>
        mapping.Children.TryGetValue(new YamlScalarNode(key), out var node) ? node : null;

    /// <summary>The refusal the person reads.</summary>
    private static PromptHeaderException Refuse(PromptHeaderMessage message, object values) =>
        new(MessageText.Format(message, values));
}
