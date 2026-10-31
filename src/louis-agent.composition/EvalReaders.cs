using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals;

namespace LouisAgent.Composition;

/// <summary>
/// Every identity's and pack's own readers that an eval file can name beside a general check, which the runner
/// gathers with the harness's plain readers. Today that is Puls's.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), beside <see cref="EvalChecks"/> and for the same reason: the composition is the one
/// project that knows every identity and pack, so the runner never names one. A reader needs only the check contract,
/// never the harness, so the composition can carry it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class EvalReaders
{
    /// <summary>Every identity's and pack's readers.</summary>
    public static IReadOnlyList<IReader> All { get; } = [.. PulsReaders.All];
}
