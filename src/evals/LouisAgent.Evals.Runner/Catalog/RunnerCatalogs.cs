using LouisAgent.Core.Llm;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// The checks and readers your evals can name, beyond the harness's general ones, which you always get. A check type or
/// reader named twice stops the runner as it starts.
/// </summary>
/// <remarks>
/// Parameter Object, for a Composition Root: your program hands the lists over at its entry point, so this library
/// never names a pack or an identity. The two lists travel together to every command that loads an eval, and grouping
/// them keeps those commands from growing a pair of parameters each.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public sealed record RunnerCatalogs(IReadOnlyList<ICheckType> Checks, IReadOnlyList<IReader> Readers)
{
    /// <summary>Only what the harness brings, for a program whose evals need nothing of their own.</summary>
    public static RunnerCatalogs GeneralOnly { get; } = new([], []);

    /// <summary>Every check type your evals can name: the general ones first, then yours.</summary>
    public CheckCatalog CheckCatalog { get; } = new([.. GeneralChecks.All, .. Checks]);

    /// <summary>Every reader your evals' checks can name: the general ones first, then yours.</summary>
    public ReaderCatalog ReaderCatalog { get; } = new([.. GeneralReaders.All, .. Readers]);

    /// <summary>The model providers the program composed, which a judge builds its client through (CL57); the host sets it.</summary>
    public ILlmClientFactory? Models { get; init; }
}
