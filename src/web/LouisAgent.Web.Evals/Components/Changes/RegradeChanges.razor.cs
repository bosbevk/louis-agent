using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Changes;

/// <summary>
/// What a re-grade did, said after it ran: that no agent was asked, which run's answers it graded, how many judge
/// calls it made and marks it kept, and line by line what changed in the file. Every figure is the run's own answer
/// against the run it re-graded (<see cref="EvalRun.CompareWith" />, ADR 0123); the part works nothing out itself.
/// </summary>
/// <remarks>
/// It says after what a re-grade spent, in place of an ask before it (Louis's pick, 2026-11-23, "No ask, the run
/// shows it after").
/// </remarks>
public partial class RegradeChanges
{
    /// <summary>The eval the runs belong to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The re-grade.</summary>
    [Parameter, EditorRequired]
    public EvalRun Run { get; set; } = null!;

    /// <summary>The run it re-graded, read whole.</summary>
    [Parameter, EditorRequired]
    public EvalRun Graded { get; set; } = null!;

    private RunChange Change => Run.CompareWith(Graded);

    private static string Title => EvalsWords.Words[EvalsPart.RegradeTitle];

    private static string Said => EvalsWords.Words[EvalsPart.RegradeSaid];

    private static string Now => EvalsWords.Words[EvalsPart.RegradeNow];

    private static string JudgeCalls => EvalsWords.Words[EvalsPart.JudgeCalls];

    private static string MarksKept => EvalsWords.Words[EvalsPart.MarksKept];

    private static string Replay => EvalsWords.Words[EvalsPart.RegradeReplay];

    private string GradedName => EvalsWords.Fill(EvalsPart.RegradeOfRun, EvalsWords.FormatLocal(Graded.StartedAt));

    private string GradedPage => EvalAddress.RunPageOf(Eval, Graded.Id);

    private string Kept => EvalsWords.Fill(EvalsPart.MetOf, Change.CriteriaKept, Run.CountCriteria().Of);

    /// <summary>
    /// The lines, in the order a person reads what happened: each criterion added with how it was judged, each taken
    /// out, the marks kept with the flips, the facts checked again, and each conversation left out.
    /// </summary>
    private IEnumerable<ChangeLine> Lines
    {
        get
        {
            var change = Change;
            foreach (var criterion in change.Criteria.Where(criterion => criterion.Added))
                yield return Added(criterion);
            foreach (var criterion in change.Criteria.Where(criterion => !criterion.Added))
                yield return new ChangeLine("−", EvalsWords.Fill(EvalsPart.CriterionTakenOut, criterion.Conversation,
                    criterion.Turn, criterion.Criterion), EvalsWords.Words[EvalsPart.CriterionTakenOutWhy]);
            yield return new ChangeLine("=", EvalsWords.Fill(EvalsPart.MarksKeptLine, change.CriteriaKept,
                Run.CountFlips()), EvalsWords.Words[EvalsPart.MarksKeptWhy]);
            yield return new ChangeLine("✓", EvalsWords.Fill(EvalsPart.FactsAgain, Run.CountFacts().Of),
                EvalsWords.Words[EvalsPart.FactsAgainWhy]);
            foreach (string conversation in change.LeftOut)
                yield return new ChangeLine("·", EvalsWords.Fill(EvalsPart.LeftOut, conversation),
                    EvalsWords.Words[EvalsPart.LeftOutWhy]);
        }
    }

    /// <summary>An added criterion, with how it was judged over its conversation's repeats.</summary>
    private ChangeLine Added(CriterionChange criterion)
    {
        var judged = Run.Conversations.First(conversation => conversation.Id == criterion.Conversation)
            .CountCriterion(criterion.Turn, criterion.Criterion);
        string repeats = EvalsWords.Count(judged.Of, EvalsPart.Repeat, EvalsPart.Repeats);
        return new ChangeLine("+", EvalsWords.Fill(EvalsPart.CriterionAdded, criterion.Conversation, criterion.Turn,
            criterion.Criterion), EvalsWords.Fill(EvalsPart.CriterionJudged, repeats, judged.Met));
    }

    /// <summary>One line of what changed: its sign, what happened, and what it means.</summary>
    private sealed record ChangeLine(string Sign, string What, string Why);
}
