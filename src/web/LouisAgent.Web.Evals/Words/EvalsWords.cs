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
