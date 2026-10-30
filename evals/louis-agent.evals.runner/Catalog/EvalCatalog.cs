using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Every eval the runner can run: the chosen sets' evals, then the stub (always). Each is checked when the catalogue is
/// built, and a refusal stops the start: a name twice, no graders, or no required fixed-rule grader (Louis,
/// 2026-10-20: no eval runs without a check that needs no model).
/// </summary>
public sealed class EvalCatalog
{
    // An eval belongs to exactly one set, and the set says whose it is (F18 step 7.5.4d); the stub belongs to none.
    private readonly IReadOnlyList<(IEval Eval, string? Identity)> _entries;
    private readonly IReadOnlyList<IEval> _evals;

    /// <summary>Gathers the sets' evals and the stub, and refuses the lot if any of them can't be trusted.</summary>
    public EvalCatalog(IEnumerable<IEvalSet> sets, IEval stub)
    {
        _entries = sets.SelectMany(set => set.Evals.Select(eval => (eval, (string?)set.Identity)))
            .Append((stub, (string?)null)).ToList();
        _evals = Check(_entries.Select(entry => entry.Eval).ToList());
    }

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
    public IReadOnlyList<ListedEval> Listing() => _entries.Select(entry => Listed(entry.Eval, entry.Identity)).ToList();

    /// <summary>
    /// An eval's line: its dataset, its scenarios' categories once each in the order they first come, and its graders.
    /// </summary>
    private static ListedEval Listed(IEval eval, string? identity) => new(
        eval.Name,
        identity,
        eval.Version,
        eval.Proves,
        eval.Manual,
        eval.Dataset is { } dataset ? new ListedDataset(dataset.Name, dataset.Version) : null,
        eval.Scenarios.SelectMany(scenario => scenario.Categories).Distinct(StringComparer.Ordinal).ToList(),
        eval.Checks.Select(grader => new ListedGrader(grader.Name, grader.Version, grader.Kind)).ToList());

    /// <summary>
    /// Refuses an eval no run could trust: a name used twice, no graders, no fixed rule, or nothing to send.
    /// </summary>
    private static IReadOnlyList<IEval> Check(IReadOnlyList<IEval> evals)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (IEval eval in evals)
        {
            if (!names.Add(eval.Name)) throw Refuse(RunnerMessage.DuplicateEval, new { Name = eval.Name });
            if (eval.Checks.Count == 0) throw Refuse(RunnerMessage.NoGraders, new { Name = eval.Name });
            if (!eval.Checks.Any(grader => grader.Required && grader.Kind == CheckKind.FixedRule))
                throw Refuse(RunnerMessage.NoFixedRuleGrader, new { Name = eval.Name });
            // A scenario with nothing to send is refused here, not skipped by the runner later.
            var empty = eval.Scenarios.FirstOrDefault(scenario => scenario.Input.Messages.Count == 0);
            if (empty is not null)
                throw Refuse(RunnerMessage.NoMessages, new
                {
                    Name = eval.Name,
                    Scenario = empty.Id
                });
        }

        return evals;
    }

    /// <summary>The refusal that stops the runner's start, with the eval it names.</summary>
    private static RunnerException Refuse(RunnerMessage message, object values) =>
        new(MessageText.Format(message, values));
}
