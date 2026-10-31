using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Readers;

/// <summary>
/// Every reader an eval file can name, found by its name, of the kind a check needs. It's built once from the readers
/// each owner lists and refuses a name used twice; a name it doesn't have, or a reader of the wrong kind, is refused
/// when a check asks for it, so a typo in a file is a refusal, never a check that reads nothing.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA) of Strategies, as <c>CheckCatalog</c> is of checks: the harness lists its plain readers
/// and an identity its own, and the runner gathers them. It holds the readers themselves (Louis's pick): a reader
/// takes no settings, so there is nothing for a factory to build. Its refusals are <see cref="InvalidDataException"/>s,
/// which the runner reports as a refusal, as the entries' own are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ReaderCatalog
{
    /// <summary>The readers, by name.</summary>
    private readonly IReadOnlyDictionary<string, IReader> _readers;

    /// <summary>Takes the readers, and refuses them if a name is used twice.</summary>
    public ReaderCatalog(IEnumerable<IReader> readers)
    {
        var byName = new Dictionary<string, IReader>(StringComparer.Ordinal);
        foreach (var reader in readers)
            if (!byName.TryAdd(reader.Name, reader))
                throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.DuplicateReader,
                    new { reader.Name }));
        _readers = byName;
    }

    /// <summary>A catalogue with no readers: for a check built outside an eval, which names none.</summary>
    public static ReaderCatalog Empty { get; } = new([]);

    /// <summary>
    /// The reader this name names, as the kind the check needs; an unknown name is refused naming the readers there
    /// are, and a reader of another kind is refused naming the kind that was needed.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public TReader Get<TReader>(string name) where TReader : class, IReader
    {
        if (!_readers.TryGetValue(name, out var reader))
            throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.UnknownReader, new
            {
                Name = name,
                Supported = _readers.Count == 0 ? "none" : string.Join(", ", _readers.Keys)
            }));
        return reader as TReader ?? throw new InvalidDataException(MessageText.Format(
            CheckCatalogMessage.WrongReaderKind, new
            {
                Name = name,
                Kind = typeof(TReader).Name.TrimStart('I')
            }));
    }
}
