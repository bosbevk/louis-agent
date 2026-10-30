namespace LouisAgent.Evals.Readers;

/// <summary>
/// Something that reads one kind of value out of an answer's text, under a name an eval file uses beside its key
/// (<c>"reader": "currency-amount"</c>). The kinds are <see cref="IMoneyReader"/>, <see cref="INumberReader"/> and
/// <see cref="IImageReader"/>.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), chosen by name from a <see cref="ReaderCatalog"/>: a generic check holds the compare and
/// the verdict, and a reader holds what a domain knows about writing the value, so an identity ships a reader, not a
/// check. It lives in the check contract (Louis's pick) so an identity can implement one without the harness.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public interface IReader
{
    /// <summary>The name an eval file uses for it, in kebab case (<c>currency-amount</c>).</summary>
    string Name { get; }
}
