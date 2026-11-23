using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs.Answers;

namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One run of an eval: which eval, against which agent, live or a replay, how many repeats, and every conversation
/// graded so far. It is what is kept in the run's file, what the API sends, and what a page draws.
/// </summary>
/// <remarks>
/// What a page asks of a run is a method here, never a property, so nothing new is written into the run's file: the
/// page's sums on the run's records, chosen over a presentation model per page (Fowler) and over static helpers in
/// the web project (Louis, 2026-11-23, by ADR 0123). It adds its repeats' answers, so each is worked out once.
/// </remarks>
/// <param name="Id">The run's id: the UTC time it started and the eval's name, as <c>20261123-043012-guide</c>.</param>
/// <param name="Eval">The eval's name, its file's name without <c>.eval.json</c>.</param>
/// <param name="Agent">The address of the agent the eval proves.</param>
/// <param name="Kind">Live, or a replay of a recording.</param>
/// <param name="Repeats">How many times each conversation is played.</param>
/// <param name="StartedAt">When the run started, in UTC.</param>
/// <param name="Conversations">The conversations graded so far, in the file's order.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record EvalRun(
    string Id,
    string Eval,
    string Agent,
    RunKind Kind,
    int Repeats,
    DateTimeOffset StartedAt,
    IReadOnlyList<ConversationRun> Conversations)
{
    /// <summary>Where the run is.</summary>
    public RunStatus Status { get; init; } = RunStatus.Running;

    /// <summary>When it ended, in UTC, once it has.</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Why it couldn't finish, when it couldn't.</summary>
    public string? StoppedBecause { get; init; }

    /// <summary>The live run a replay plays back, or none for a live run.</summary>
    public string? ReplayOf { get; init; }

    /// <summary>Whether the page a visitor is on was sent with each ask; never yet, so a run says so plainly.</summary>
    public bool PageSent { get; init; }

    /// <summary>How many of its conversations passed.</summary>
    public int Passed => Conversations.Count(conversation => conversation.Passed);

    /// <summary>Its facts over every repeat of every conversation, met of all.</summary>
    public MarkCount CountFacts() => AllRepeats().Aggregate(MarkCount.None, (sum, repeat) => sum + repeat.CountFacts());

    /// <summary>Its criteria over every repeat of every conversation, met of all.</summary>
    public MarkCount CountCriteria() =>
        AllRepeats().Aggregate(MarkCount.None, (sum, repeat) => sum + repeat.CountCriteria());

    /// <summary>What judging every turn of every repeat cost, the known ones added; null when none is known.</summary>
    public decimal? SumJudgeCost() =>
        KnownCost.Sum(AllRepeats().SelectMany(repeat => repeat.Turns).Select(turn => turn.JudgeCost));

    /// <summary>What the agent's turns cost, the known ones added; null when none has a price.</summary>
    public decimal? SumCost() =>
        KnownCost.Sum(AllRepeats().SelectMany(repeat => repeat.Turns).Select(turn => turn.Cost));

    /// <summary>How long it took from its start to its end; none while it has no end.</summary>
    public TimeSpan? MeasureTime() => EndedAt - StartedAt;

    /// <summary>How long it has gone by this moment: to now while it goes, to its end once it has ended.</summary>
    public TimeSpan MeasureTimeSoFar(DateTimeOffset now) => (EndedAt ?? now) - StartedAt;

    /// <summary>The currency its costs are in, from its first priced turn; none while no turn is priced.</summary>
    public string? FindCurrency() =>
        AllRepeats().SelectMany(repeat => repeat.Turns).Select(turn => turn.Currency)
            .FirstOrDefault(c => c is not null);

    /// <summary>
    /// Its conversations with the failed ones first, then those that passed only after a flip, then the rest, and
    /// inside each group as the file has them; the filters keep the order, so no sort can swap two that are equal.
    /// </summary>
    public IReadOnlyList<ConversationRun> OrderFailedFirst() =>
    [
        .. Conversations.Where(conversation => !conversation.Passed),
        .. Conversations.Where(conversation => conversation.PassedAfterAFlip()),
        .. Conversations.Where(conversation => conversation.Passed && !conversation.PassedAfterAFlip())
    ];

    /// <summary>The next conversation that didn't pass after this one, in that order; none after the last.</summary>
    public ConversationRun? FindNextFailed(string after) =>
        OrderFailedFirst().SkipWhile(conversation => conversation.Id != after).Skip(1)
            .FirstOrDefault(conversation => !conversation.Passed);

    /// <summary>The conversation before this one, in that order; none before the first or for an unknown id.</summary>
    public ConversationRun? FindPrevious(string before) =>
        OrderFailedFirst().TakeWhile(conversation => conversation.Id != before).LastOrDefault();

    /// <summary>How many marks a person flipped, over every repeat of every conversation.</summary>
    public int CountFlips() => AllRepeats().Sum(repeat => repeat.CountFlips());

    /// <summary>
    /// This run with one criterion flipped, or put back with none: the conversation by its id, the repeat by its
    /// number, and the turn and the criterion counted from 1, as the pages show them. None for a place it doesn't have.
    /// </summary>
    public EvalRun? WithFlip(string conversation, int repeat, int turn, int criterion, Flip? flip)
    {
        int at = Conversations.ToList().FindIndex(played => played.Id == conversation);
        if (at < 0 || Conversations[at].WithFlip(repeat, turn, criterion, flip) is not { } flipped) return null;
        return this with { Conversations = TurnRun.Replace(Conversations, at, flipped) };
    }

    /// <summary>Every repeat of every conversation.</summary>
    private IEnumerable<RepeatRun> AllRepeats() => Conversations.SelectMany(conversation => conversation.Repeats);
}
