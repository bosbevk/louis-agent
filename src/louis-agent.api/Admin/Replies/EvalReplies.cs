using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>
/// Builds the eval list's reply from the catalogue and the store's runs: each contract record gets its <c>Of</c> here,
/// so the handler reads <c>EvalListReply.Of(…)</c> and never spells out a row's fields itself.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, as <c>AdminReplies</c>: the
/// records sit in the contract, which can't see a check or a stored run, so their factories sit here beside those
/// types.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class EvalReplies
{
    /// <summary>The whole list is gathered here, so its endpoint stays a few lines.</summary>
    extension(EvalListReply)
    {
        /// <summary>
        /// Every eval in the catalogue, with its runs where the store gave them, and the store's state. With no runs
        /// to hand (no store, or it didn't answer) nobody knows whether an eval ran.
        /// </summary>
        public static EvalListReply Of(BenchmarkCatalog catalog, string workspaceRoot,
            IReadOnlyDictionary<string, IReadOnlyList<BenchmarkDocuments.Run>>? runs, StoreRow store) =>
            new([
                .. catalog.Checks.Select(check => EvalRow.Of(check, BenchmarkManuals.Has(workspaceRoot, check),
                    runs?.GetValueOrDefault(check.Id)))
            ], store);
    }

    /// <summary>
    /// An eval's row puts what the runner listed beside what its runs show: the modes and models it ran on.
    /// </summary>
    extension(EvalRow)
    {
        /// <summary>
        /// The row for one eval: what it is from the runner's line, and its runs, newest first. It never ran when the
        /// store has none of its runs.
        /// </summary>
        public static EvalRow Of(BenchmarkCheck check, bool manual, IReadOnlyList<BenchmarkDocuments.Run>? runs)
        {
            // No runs to hand means we don't know; the modes and models below then come out empty.
            var known = runs ?? [];

            return new EvalRow(
                check.Id,
                check.Eval?.Identity,
                check.Eval?.Version,
                check.Title,
                manual,
                // What the eval is comes from the runner's line; a Python check has none of it.
                check.Eval?.Dataset is { } dataset ? new EvalDatasetRow(dataset.Name, dataset.Version) : null,
                check.Eval?.Categories ?? [],
                [.. (check.Eval?.Graders ?? []).Select(g => new EvalGraderRow(g.Name, g.Version, g.Kind.Wire()))],
                // The modes and models it ran on, each named once, from its runs.
                [
                    .. known.SelectMany(run => run.Modes ?? []).Select(mode => mode.Name)
                        .Distinct(StringComparer.Ordinal)
                ],
                [
                    .. known.SelectMany(run => run.Models ?? []).Select(use => use.Model).OfType<string>()
                        .Distinct(StringComparer.Ordinal)
                ],
                // "Never ran" is only said when the store answered; without its runs it stays unknown.
                runs is null ? null : runs.Count == 0,
                [.. known.Select(EvalRunRow.Of)]);
        }
    }

    /// <summary>A run in the list is the stored run's own row, so the list and the run page read it alike.</summary>
    extension(EvalRunRow)
    {
        /// <summary>The row for one stored run, with each mode and its rates.</summary>
        public static EvalRunRow Of(BenchmarkDocuments.Run run) =>
            new(StoredRunRow.Of(run), [.. (run.Modes ?? []).Select(ModeRow.Of)]);
    }
}
