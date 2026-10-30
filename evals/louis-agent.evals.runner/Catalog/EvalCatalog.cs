using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Every eval the runner can run: the chosen evals, each a folder of files, then the stub (always). Each is checked
/// when the catalogue is built, and a refusal stops the start: a name twice, no checks, or no required fixed-rule
/// check (Louis, 2026-10-20: no eval runs without a check that needs no model).
/// </summary>
public sealed class EvalCatalog
{
    /// <summary>The evals, checked, in the catalogue's order; each says whose it is, the stub nobody's.</summary>
    private readonly IReadOnlyList<IEval> _evals;

    /// <summary>Gathers the evals and the stub, and refuses the lot if any of them can't be trusted.</summary>
    public EvalCatalog(IEnumerable<IEval> evals, IEval stub) => _evals = Check([.. evals, stub]);

    /// <summary>Every eval, in the catalogue's order.</summary>
    public IReadOnlyList<IEval> Evals => _evals;

    /// <summary>The eval with this name, if there is one.</summary>
    public IEval? Find(string name) =>
        _evals.FirstOrDefault(eval => string.Equals(eval.Name, name, StringComparison.Ordinal));

    /// <summary>The eval a name names; a name no eval has is refused, naming the evals there are.</summary>
    public IEval Get(string name) =>
        Find(name) ?? throw new RunnerException(MessageText.Format(RunnerMessage.UnknownEval,
            new
            {
                Name = name,
                Supported = string.Join(", ", _evals.Select(eval => eval.Name))
            }));

    /// <summary>
    /// One line per eval, in the catalogue's order: what the API's catalogue reads, with what the portal shows
    /// before an eval ever ran.
    /// </summary>
    public IReadOnlyList<ListedEval> Listing() => [.. _evals.Select(Listed)];

    /// <summary>
    /// An eval's line: whose it is, its dataset, its scenarios' categories once each in the order they first come, and
    /// its checks.
    /// </summary>
    private static ListedEval Listed(IEval eval) => new(
        eval.Name,
        eval.Identity,
        eval.Version,
        eval.Proves,
        eval.Manual,
        eval.Dataset is { } dataset ? new ListedDataset(dataset.Name, dataset.Version) : null,
        eval.Scenarios.SelectMany(scenario => scenario.Categories).Distinct(StringComparer.Ordinal).ToList(),
        eval.Checks.Select(grader => new ListedGrader(grader.Name, grader.Version, grader.Kind)).ToList());

    /// <summary>
    /// Refuses an eval no run could trust: a name used twice, no checks, no fixed rule, or nothing to send.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private static IReadOnlyList<IEval> Check(IReadOnlyList<IEval> evals)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var eval in evals)
        {
            if (!names.Add(eval.Name)) throw Refuse(RunnerMessage.DuplicateEval, new { eval.Name });
            if (eval.Checks.Count == 0) throw Refuse(RunnerMessage.NoGraders, new { eval.Name });
            if (!eval.Checks.Any(check => check is { Required: true, Kind: CheckKind.FixedRule }))
                throw Refuse(RunnerMessage.NoFixedRuleGrader, new { eval.Name });
            // A scenario with nothing to send is refused here, not skipped by the runner later.
            var empty = eval.Scenarios.FirstOrDefault(scenario => scenario.Input.Messages.Count == 0);
            if (empty is not null)
                throw Refuse(RunnerMessage.NoMessages, new
                {
                    eval.Name,
                    Scenario = empty.Id
                });
        }

        return evals;
    }

    /// <summary>The refusal that stops the runner's start, with the eval it names.</summary>
    private static RunnerException Refuse(RunnerMessage message, object values) =>
        new(MessageText.Format(message, values));
}
