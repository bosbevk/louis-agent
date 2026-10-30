using System.Text.Json.Schema;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// Every check type an eval file can name, found by its type. It's built once from the entries each owner lists,
/// and refuses a list it can't trust.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), filled with Plugin entries: the harness lists its general checks and a pack will list
/// its own, and the composition gathers them here, so no part has to know every check.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class CheckCatalog
{
    private readonly IReadOnlyList<ICheckType> _entries;

    /// <summary>Takes the entries, and refuses them if any type is unsafe or used twice.</summary>
    public CheckCatalog(IEnumerable<ICheckType> entries) => _entries = Check(entries.ToList());

    /// <summary>The entry with this type, if there is one.</summary>
    public ICheckType? Find(string type) =>
        _entries.FirstOrDefault(entry => string.Equals(entry.Type, type, StringComparison.Ordinal));

    /// <summary>The entry a type names; a type the catalogue doesn't have is refused, naming the ones it has.</summary>
    public ICheckType Get(string type) =>
        Find(type) ?? throw Refuse(CheckCatalogMessage.UnknownType, new
        {
            Type = type,
            Supported = string.Join(", ", _entries.Select(entry => entry.Type))
        });

    /// <summary>One line per entry, in the catalogue's order: its type, its kind and its settings' schema.</summary>
    public IReadOnlyList<ListedCheck> Listing() =>
        _entries.Select(entry => new ListedCheck(entry.Type, entry.Kind,
            EvalJson.Strict.GetJsonSchemaAsNode(entry.Parameters))).ToList();

    /// <summary>
    /// Refuses a list a file couldn't name from: a type an MCP tool name can't carry (an empty one included), or a
    /// type listed twice.
    /// </summary>
    private static IReadOnlyList<ICheckType> Check(IReadOnlyList<ICheckType> entries)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (ICheckType entry in entries)
        {
            if (!IsMcpName(entry.Type))
                throw Refuse(CheckCatalogMessage.TypeNotSafe, new { entry.Type });
            if (!types.Add(entry.Type)) throw Refuse(CheckCatalogMessage.DuplicateType, new { entry.Type });
        }

        return entries;
    }

    /// <summary>The refusal with its text filled in.</summary>
    private static CheckCatalogException Refuse(CheckCatalogMessage message, object values) =>
        new(MessageText.Format(message, values));

    /// <summary>
    /// True when the type is one an MCP tool name can carry, as the conformance check holds an eval's name: ASCII
    /// letters, digits, '-' and '_', one to 64 of them ("within-time" is; "" and "time.limit" aren't).
    /// </summary>
    private static bool IsMcpName(string type) =>
        type.Length is >= 1 and <= 64 && type.All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '-' or '_');
}
