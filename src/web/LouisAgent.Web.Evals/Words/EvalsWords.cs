using System.Globalization;
using LouisAgent.Composition.Words;
using LouisAgent.Web.Evals.Words.Parts;

namespace LouisAgent.Web.Evals.Words;

/// <summary>
/// The words the eval pages' code picks, one entry a part, and how a count and a time are said on them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class EvalsWords
{
    /// <summary>Each part's words.</summary>
    public static readonly PartWords<EvalsPart, string> Words = PartWords<EvalsPart, string>.Of(part => part switch
    {
        EvalsPart.Conversation => "conversation",
        EvalsPart.Conversations => "conversations",
        EvalsPart.Repeat => "repeat",
        EvalsPart.Repeats => "repeats",
        EvalsPart.Met => "met",
        EvalsPart.NotMet => "not met",
        EvalsPart.FactNotMet => "fact not met",
        EvalsPart.Unknown => "unknown",
        EvalsPart.ToolCall => "tool call",
        EvalsPart.ToolCalls => "tool calls",
        EvalsPart.NotRunYet => "Not run yet.",
        EvalsPart.CouldNotFinish => "Could not finish",
        EvalsPart.ReplayFree => "Replay free",
        EvalsPart.RunLiveAsk => "Run live…",
        EvalsPart.NothingToReplay => "Nothing to replay yet. A replay plays a live run back.",
        EvalsPart.AskQuestion => "Run {0} live?",
        EvalsPart.AskWhat => "{0}, against the real model and the real site.",
        EvalsPart.RepeatsLabel => "Repeats",
        EvalsPart.Estimate => "About {0} for {1}, from what the last live run cost.",
        EvalsPart.CostNotKnown => "The cost isn't known yet. No live run with a price has finished.",
        EvalsPart.RunLiveAbout => "Run live, about {0}",
        EvalsPart.RunLive => "Run live",
        EvalsPart.DontRun => "Don't run",
        EvalsPart.GoingBefore => "A run of",
        EvalsPart.GoingAfter => "is going. One run goes at a time, so the others wait.",
        EvalsPart.GoingSince => "Going since {0}",
        EvalsPart.FollowTheRun => "Follow the run →",
        EvalsPart.Stopped => "Stopped",
        EvalsPart.SoFar => "So far",
        EvalsPart.Took => "Took",
        EvalsPart.AgentCostName => "The agent's cost",
        EvalsPart.JudgeCostName => "The judge's cost",
        EvalsPart.StopTheRun => "Stop the run",
        EvalsPart.RunGoingHead =>
            "The run is going. {0} of {1} conversations are graded so far, and this page reads it again every few " +
            "seconds.",
        EvalsPart.RunStoppedHead => "{0} of {1} conversations were graded, then the run was stopped.",
        EvalsPart.StoppedTitle => "The run was stopped",
        EvalsPart.StoppedByAPerson => "A person stopped it. What was graded before is kept.",
        EvalsPart.DidNotRun => "{0} did not run.",
        EvalsPart.Flip => "Flip",
        EvalsPart.FlipToMet => "Flip to met",
        EvalsPart.FlipToNotMet => "Flip to not met",
        EvalsPart.FlipCounts => "Your answer counts in place of the judge's, and the judge's stays beside it.",
        EvalsPart.FlipNoteLabel => "Why, in one line, if you want to say",
        EvalsPart.DontFlip => "Don't flip",
        EvalsPart.FlippedTo => "You flipped this to {0}.",
        EvalsPart.JudgeSaid => "The judge said {0}: \"{1}\"",
        EvalsPart.YourNote => "Your note: {0}",
        EvalsPart.UndoTheFlip => "Undo the flip",
        EvalsPart.PassedAfterYourFlip => "Passed after your flip",
        EvalsPart.Passed => "Passed",
        EvalsPart.Failed => "Failed",
        EvalsPart.ConversationHead => "{0}. {1} of {2} passed. This is repeat {3}.",
        EvalsPart.YourFlips => "Your flips",
        EvalsPart.FactInCode => "Fact, checked in code",
        EvalsPart.TheJudge => "The judge: {0}",
        EvalsPart.ThisRepeat => "This repeat",
        EvalsPart.FactsMet => "Facts met",
        EvalsPart.CriteriaMet => "Criteria met",
        EvalsPart.MetOf => "{0} of {1}",
        EvalsPart.Previous => "‹ Previous",
        EvalsPart.NextFailed => "Next failed ›",
        EvalsPart.Reload => "Reload",
        EvalsPart.InTheRunBefore => "Open this conversation in the run before",
        EvalsPart.NoRun => "This host has no run {0}.",
        EvalsPart.NoConversation => "This run has no conversation {0}.",
        EvalsPart.NoRepeat => "This conversation has no repeat {0}.",
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
    });

    /// <summary>A count in words, in the one's words for one and the many's for any other: "3 repeats".</summary>
    public static string Count(int count, EvalsPart one, EvalsPart many) =>
        $"{count} {Words[count == 1 ? one : many]}";

    /// <summary>A part's words with its holes filled, in order, the same in every culture.</summary>
    public static string Fill(EvalsPart part, params object[] holes) =>
        string.Format(CultureInfo.InvariantCulture, Words[part], holes);

    /// <summary>A time in the browser's own time, as the overview shows a talk's: "23 Nov 2026, 08:19".</summary>
    public static string FormatLocal(DateTimeOffset at) =>
        at.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// A time in its two largest parts, as a person says it: "33 s", "6 min 12 s", "1 h 2 min".
    /// </summary>
    /// <remarks>A switch on the time's shape, one arm a size, so the three cases read as a table (ADR 0051).</remarks>
    public static string FormatTime(TimeSpan time) => time switch
    {
        { TotalHours: >= 1 } => $"{(int)time.TotalHours} h {time.Minutes} min",
        { TotalMinutes: >= 1 } => $"{time.Minutes} min {time.Seconds} s",
        _ => $"{time.Seconds} s"
    };
}
