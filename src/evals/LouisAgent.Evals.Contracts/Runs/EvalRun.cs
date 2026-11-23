namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One run of an eval: which eval, against which agent, live or a replay, how many repeats, and every conversation
/// graded so far. It is what is kept in the run's file, what the API sends, and what a page draws.
/// </summary>
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
}
