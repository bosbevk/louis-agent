using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Every eval the runner can run: the chosen sets' evals, then the stub (always). Each is checked when the catalogue is built,
/// and a refusal stops the start: a name twice, no graders, or no required fixed-rule grader (Louis, 2026-10-20: no eval runs
/// without a check that needs no model).
/// </summary>
public sealed class EvalCatalog(IEnumerable<IEvalSet> sets, IEval stub)
{
    private readonly IReadOnlyList<IEval> _evals = Check(sets.SelectMany(set => set.Evals).Append(stub).ToList());

    public IReadOnlyList<IEval> Evals => _evals;

    public IEval? Find(string name) => _evals.FirstOrDefault(eval => string.Equals(eval.Name, name, StringComparison.Ordinal));

    /// <summary>The eval a name names; a name no eval has is refused, naming the evals there are.</summary>
    public IEval Get(string name) =>
        Find(name) ?? throw new RunnerException(MessageText.Format(RunnerMessage.UnknownEval,
            new { Name = name, Supported = string.Join(", ", _evals.Select(eval => eval.Name)) }));

    /// <summary>One line per eval, in the catalogue's order: what the API's catalogue reads (F18 step 4.3).</summary>
    public IReadOnlyList<ListedEval> Listing() =>
        _evals.Select(eval => new ListedEval(eval.Name, eval.Identity, eval.Version, eval.Proves)).ToList();

    private static IReadOnlyList<IEval> Check(IReadOnlyList<IEval> evals)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (IEval eval in evals)
        {
            if (!names.Add(eval.Name)) throw Refuse(RunnerMessage.DuplicateEval, new { Name = eval.Name });
            if (eval.Graders.Count == 0) throw Refuse(RunnerMessage.NoGraders, new { Name = eval.Name });
            if (!eval.Graders.Any(grader => grader.Required && grader.Kind == GraderKind.FixedRule))
                throw Refuse(RunnerMessage.NoFixedRuleGrader, new { Name = eval.Name });
            foreach (var scenario in eval.Scenarios)
                if (scenario.Input.Messages.Count == 0) throw Refuse(RunnerMessage.NoMessages, new { Name = eval.Name, Scenario = scenario.Id });
        }
        return evals;
    }

    private static RunnerException Refuse(RunnerMessage message, object values) => new(MessageText.Format(message, values));
}
