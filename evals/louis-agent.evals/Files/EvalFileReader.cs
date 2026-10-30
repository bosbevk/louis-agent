using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Files;

/// <summary>
/// Reads an eval folder's <c>eval.json</c> into an <see cref="EvalFile"/>, and turns away a file it can't trust: no
/// file, one that doesn't fit the records, another schema version, no modes or no checks, or a name used twice.
/// </summary>
/// <remarks>
/// The first half of a Factory Method: it makes the record, and the eval is made from the record later (F49.2 step
/// 4). It reads with <see cref="EvalJson.Strict"/>, so an unknown, missing or null field is refused by the reader
/// itself, naming the field, and a typo can't become a default. Each check after that is a guard clause (ADR 0051).
/// Whether a name suits MCP is the catalogue's and the conformance check's to say, not this reader's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public static class EvalFileReader
{
    /// <summary>The file every eval folder holds.</summary>
    private const string FileName = "eval.json";

    /// <summary>The one shape of the file this reader knows.</summary>
    private const int Schema = 1;

    /// <summary>Reads the eval in the folder, or refuses it with what to fix.</summary>
    public static EvalFile Read(string folder)
    {
        var file = Path.Combine(folder, FileName);
        if (!File.Exists(file)) throw Refuse(EvalFileMessage.NoFile, new { Folder = folder });

        var eval = Parse(file);
        if (eval.SchemaVersion != Schema)
            throw Refuse(EvalFileMessage.SchemaVersion, new
            {
                File = file,
                Version = eval.SchemaVersion
            });
        if (eval.Modes.Count == 0) throw Refuse(EvalFileMessage.NoModes, new { File = file });
        if (eval.Checks.Count == 0) throw Refuse(EvalFileMessage.NoChecks, new { File = file });
        if (Twice(eval.Modes.Select(entry => entry.Name)) is { } mode)
            throw Refuse(EvalFileMessage.ModeTwice, new
            {
                File = file,
                Name = mode
            });
        if (Twice(eval.Checks.Select(entry => entry.Name ?? entry.Type)) is { } check)
            throw Refuse(EvalFileMessage.CheckTwice, new
            {
                File = file,
                Name = check
            });
        return eval;
    }

    /// <summary>
    /// The file read strictly into the record; the JSON reader's own reason, which names the field, goes into the
    /// refusal.
    /// </summary>
    private static EvalFile Parse(string file)
    {
        try
        {
            return JsonSerializer.Deserialize<EvalFile>(File.ReadAllText(file), EvalJson.Strict)
                   ?? throw Refuse(EvalFileMessage.Empty, new { File = file });
        }
        catch (JsonException error)
        {
            throw Refuse(EvalFileMessage.Unreadable, new
            {
                File = file,
                Reason = error.Message
            });
        }
    }

    /// <summary>The first name that comes up more than once, or null when each is used once.</summary>
    private static string? Twice(IEnumerable<string> names) =>
        names.GroupBy(name => name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1)?.Key;

    /// <summary>The refusal with its text filled in.</summary>
    private static InvalidDataException Refuse(EvalFileMessage message, object values) =>
        new(MessageText.Format(message, values));
}
