using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Transcripts;

namespace LouisAgent.Evals.Grading.Turns;

/// <summary>
/// Grades one turn: each fact told by code from its transcript and the turns before it, and each criterion marked by
/// the judge from the turns so far. Given the same turn as an earlier run graded it, a criterion whose turn and words
/// are the same keeps its earlier mark, flip and note with it, and the judge isn't asked.
/// </summary>
/// <remarks>
/// Extract Class (Fowler): the player's grading moved here so the player and the re-grader grade a turn the one way,
/// chosen over a public method on the player and over a static class that calls the judge (Louis's pick, 2026-11-23,
/// "TurnGrader, own folder"). A kept mark costs a known 0, so a turn graded with no judge call costs the judge 0.
/// </remarks>
/// <param name="judge">The judge that marks each criterion it isn't given.</param>
/// <seealso href="https://refactoring.com/catalog/extractClass.html">Fowler: Extract Class</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class TurnGrader(Judge judge)
{
    /// <summary>
    /// The turn's marks, as the file asks now: its facts, and its criteria, each kept from <paramref name="before" />
    /// where <see cref="MarkReuse" /> finds it, the rest marked by the judge. The agent's cost is the one given.
    /// </summary>
    /// <param name="turn">The turn as the eval file has it now.</param>
    /// <param name="transcript">What happened in it.</param>
    /// <param name="cost">What the agent's turn cost.</param>
    /// <param name="soFar">Every turn up to and including this one.</param>
    /// <param name="before">The same turn as an earlier run graded it, or none.</param>
    /// <param name="cancellationToken">Stops the judge's calls.</param>
    public async Task<TurnRun> GradeAsync(Turn turn, TurnTranscript transcript, TurnCost cost,
        IReadOnlyList<TurnTranscript> soFar, TurnRun? before, CancellationToken cancellationToken)
    {
        var earlier = soFar.Take(soFar.Count - 1).ToList();
        var facts = turn.Facts.Select(fact => Facts.Tell(fact, transcript, earlier)).ToList();

        var criteria = new List<Mark>();
        var judgeCosts = new List<decimal?>();
        foreach (string criterion in turn.Criteria)
        {
            // A mark kept from before costs nothing: the judge isn't asked again.
            if (before is not null && MarkReuse.FindReusable(before, transcript, criterion) is { } kept)
            {
                criteria.Add(kept);
                judgeCosts.Add(0m);
                continue;
            }

            var judged = await judge.MarkAsync(criterion, soFar, cancellationToken);
            criteria.Add(judged.Mark);
            judgeCosts.Add(judged.Cost);
        }

        return new TurnRun(transcript, facts, criteria, cost.Amount, cost.Currency)
        {
            JudgeCost = KnownCost.Sum(judgeCosts)
        };
    }
}
