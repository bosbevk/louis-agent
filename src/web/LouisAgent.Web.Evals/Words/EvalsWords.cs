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
        EvalsPart.Regrade => "Re-grade",
        EvalsPart.RegradeTitle => "What changed in the file, and what it did",
        EvalsPart.RegradeSaid => "No agent was asked: these are the answers of",
        EvalsPart.RegradeOfRun => "the run of {0}",
        EvalsPart.RegradeNow => ", graded with the eval file as it is now.",
        EvalsPart.JudgeCalls => "Judge calls",
        EvalsPart.MarksKept => "Marks kept",
        EvalsPart.CriterionAdded => "{0} · turn {1} · a new criterion, \"{2}\"",
        EvalsPart.CriterionJudged => "Judged in each of {0}: met in {1}.",
        EvalsPart.CriterionTakenOut => "{0} · turn {1} · \"{2}\" was taken out",
        EvalsPart.CriterionTakenOutWhy => "Your flips on it stay with the run it re-graded.",
        EvalsPart.MarksKeptLine => "{0} marks kept, and your {1} flips with them",
        EvalsPart.MarksKeptWhy =>
            "Same criterion, same answer: the judge's mark and your flip carry over, free. A reworded criterion is " +
            "judged again.",
        EvalsPart.FactsAgain => "{0} facts checked again",
        EvalsPart.FactsAgainWhy => "Facts are code: always checked again, always free.",
        EvalsPart.LeftOut => "{0} was left out",
        EvalsPart.LeftOutWhy => "The run it re-graded has no answer to it as the file asks it now.",
        EvalsPart.RegradeReplay =>
            "Replay free plays this again: the judge's new answers were added to the recording of the live run.",
        EvalsPart.ChangeMore => "▲ {0} more than the run before",
        EvalsPart.ChangeFewer => "▼ {0} fewer than the run before",
        EvalsPart.ChangeSame => "the same as the run before",
        EvalsPart.NowPasses => "{0} now passes",
        EvalsPart.NowFails => "{0} now fails",
        EvalsPart.NowPassesTag => "now passes",
        EvalsPart.NowFailsTag => "now fails",
        EvalsPart.ConversationsTitle => "Conversations",
        EvalsPart.LastRuns => "The last {0} runs",
        EvalsPart.TrendNote => "Conversations passed, oldest first, finished runs only.",
        EvalsPart.MissedMost => "The criteria missed most",
        EvalsPart.MissedIn => "missed in {0} of {1}",
        EvalsPart.FlippedIn => "you flipped it in {0} of {1}",
        EvalsPart.KeepFlipping => "A criterion you keep flipping wants rewording, or to be a fact.",
        EvalsPart.FlippedLately => "You flipped this criterion in {0} of the last {1} runs.",
        EvalsPart.CopyAsConversation => "Copy as conversation",
        EvalsPart.CopiedMark => "Copied ✓",
        EvalsPart.Copied => "Copied: this conversation as a block for {0}.eval.json, ready to paste and change.",
        EvalsPart.CopyRefused => "The browser didn't allow the copy. Allow the clipboard for this site and try again.",
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
    });

    /// <summary>A count in words, in the one's words for one and the many's for any other: "3 repeats".</summary>
    public static string Count(int count, EvalsPart one, EvalsPart many) =>
        $"{count} {Words[count == 1 ? one : many]}";

    /// <summary>
    /// A change against the run before in words, by its sign: "▲ 1 more than the run before", "▼ 2 fewer…", or "the
    /// same as the run before" (F126.4).
    /// </summary>
    /// <remarks>A switch on the change's sign, one arm a case, read as a table (ADR 0051).</remarks>
    public static string SayChange(int change) => change switch
    {
        > 0 => Fill(EvalsPart.ChangeMore, change),
        < 0 => Fill(EvalsPart.ChangeFewer, -change),
        _ => Words[EvalsPart.ChangeSame]
    };

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
