namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>
/// A run against the run before it: how many conversations passed then and now, which now pass and which now fail,
/// the criteria added and taken out, how many marks were kept as they were, and the conversations left out. A page
/// asks the run for it (<see cref="EvalRun.CompareWith" />) and works nothing out itself (ADR 0123).
/// </summary>
/// <param name="PassedBefore">How many conversations the run before passed.</param>
/// <param name="PassedNow">How many this run passed.</param>
/// <param name="NowPass">The conversations both hold that failed before and pass now, in this run's order.</param>
/// <param name="NowFail">Those that passed before and fail now.</param>
/// <param name="Criteria">Each criterion added to a turn or taken out of one, in this run's order.</param>
/// <param name="CriteriaKept">How many criterion marks, over every repeat, are the same as before.</param>
/// <param name="CriteriaJudged">How many are not: for a re-grade, the judge's calls it made.</param>
/// <param name="LeftOut">The conversations the run before held that this run doesn't.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record RunChange(
    int PassedBefore,
    int PassedNow,
    IReadOnlyList<string> NowPass,
    IReadOnlyList<string> NowFail,
    IReadOnlyList<CriterionChange> Criteria,
    int CriteriaKept,
    int CriteriaJudged,
    IReadOnlyList<string> LeftOut)
{
    /// <summary>This run against the one before it, conversation by conversation where both hold one.</summary>
    public static RunChange Of(EvalRun now, EvalRun before)
    {
        var both = now.Conversations
            .Select(conversation => (Now: conversation, Before: before.Conversations
                .FirstOrDefault(earlier => earlier.Id == conversation.Id)))
            .Where(pair => pair.Before is not null)
            .Select(pair => (pair.Now, Before: pair.Before!))
            .ToList();
        var held = now.Conversations.Select(conversation => conversation.Id).ToHashSet();
        int kept = both.Sum(pair => CountKept(pair.Now, pair.Before));
        return new RunChange(
            before.Passed,
            now.Passed,
            [.. both.Where(pair => pair.Now.Passed && !pair.Before.Passed).Select(pair => pair.Now.Id)],
            [.. both.Where(pair => !pair.Now.Passed && pair.Before.Passed).Select(pair => pair.Now.Id)],
            [.. both.SelectMany(pair => CriteriaChanged(pair.Now, pair.Before))],
            kept,
            now.CountCriteria().Of - kept,
            [.. before.Conversations.Select(conversation => conversation.Id).Where(id => !held.Contains(id))]);
    }

    /// <summary>
    /// The criteria of each turn, as its first repeat has them, added since the run before and then taken out.
    /// </summary>
    private static IEnumerable<CriterionChange> CriteriaChanged(ConversationRun now, ConversationRun before)
    {
        var nowTurns = CriteriaOf(now);
        var beforeTurns = CriteriaOf(before);
        for (int at = 0; at < Math.Max(nowTurns.Count, beforeTurns.Count); at++)
        {
            var added = at < nowTurns.Count ? nowTurns[at] : [];
            var taken = at < beforeTurns.Count ? beforeTurns[at] : [];
            foreach (string criterion in added.Except(taken))
                yield return new CriterionChange(now.Id, at + 1, criterion, Added: true);
            foreach (string criterion in taken.Except(added))
                yield return new CriterionChange(now.Id, at + 1, criterion, Added: false);
        }
    }

    /// <summary>Each turn's criteria, by their words, as the conversation's first repeat has them.</summary>
    private static List<List<string>> CriteriaOf(ConversationRun conversation) =>
        conversation.Repeats.FirstOrDefault()?.Turns
            .Select(turn => turn.Criteria.Select(mark => mark.Text).ToList()).ToList() ?? [];

    /// <summary>
    /// How many criterion marks are the same as in the run before, mark for mark, the same repeat and turn.
    /// </summary>
    private static int CountKept(ConversationRun now, ConversationRun before) =>
        now.Repeats.Sum(repeat => before.FindRepeat(repeat.Number) is { } earlier
            ? repeat.Turns.Zip(earlier.Turns).Sum(turns => turns.First.Criteria.Count(turns.Second.Criteria.Contains))
            : 0);
}
