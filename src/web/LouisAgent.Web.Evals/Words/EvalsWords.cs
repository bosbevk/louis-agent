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
}
