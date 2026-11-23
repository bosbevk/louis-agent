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
        EvalsPart.NotRunYet => "Not run yet.",
        EvalsPart.CouldNotFinish => "Could not finish",
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
    });

    /// <summary>A count in words, in the one's words for one and the many's for any other: "3 repeats".</summary>
    public static string Count(int count, EvalsPart one, EvalsPart many) =>
        $"{count} {Words[count == 1 ? one : many]}";

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
