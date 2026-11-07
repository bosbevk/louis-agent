using System.Globalization;

namespace LouisAgent.Packs.Recipes.Recipes.Tidying;

/// <summary>
/// One step as the steps it really holds. Taste's data keeps a whole method in one, <c>1. Heat the pan.2. Mix it.</c>,
/// and a cook writing from a paragraph that long drops clauses: the garlic, the ginger.
/// </summary>
/// <remarks>
/// A pipeline of three plain string walks, not regular expressions (ADR 0057), from the surest cut to the least sure:
/// the page's own numbers, paragraphs glued with no space, then the sentences of what's still long. Numbers only count
/// in order, right after a sentence, so <c>3 cups</c> in the middle of a step is never taken for step three.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
internal static class StepParts
{
    /// <summary>A step over this is too long to follow, so it's cut at its sentences.</summary>
    private const int LongStep = 400;

    /// <summary>A part of a long step holds at least this much, so it isn't cut into single sentences.</summary>
    private const int EnoughForAPart = 200;

    /// <summary>The parts of a step, every word kept.</summary>
    public static IEnumerable<string> Of(string step) =>
        Numbered(step.Trim()).SelectMany(Glued).SelectMany(Sentences);

    /// <summary>A method numbered inside one step, without its numbers. One not numbered in order stays.</summary>
    private static List<string> Numbered(string text)
    {
        if (MarkerAt(text, 0, 1) is not { } first) return [text];

        var parts = new List<string>();
        int start = first;
        int next = 2;
        for (int at = first; at < text.Length; at++)
        {
            if (!AfterASentence(text, at) || MarkerAt(text, at, next) is not { } length) continue;

            parts.Add(text[start..at].Trim());
            start = at + length;
            at = start - 1;
            next++;
        }

        if (parts.Count == 0) return [text];
        parts.Add(text[start..].Trim());
        return parts;
    }

    /// <summary>How long this step's number is here, up to the capital that starts its words.</summary>
    /// <remarks>
    /// Guard clauses (ADR 0051). <c>2. Mix</c>, <c>2.Mix</c> and <c>2 Mix</c> are numbers; <c>2 cups</c> isn't.
    /// </remarks>
    private static int? MarkerAt(string text, int at, int number)
    {
        string digits = number.ToString(CultureInfo.InvariantCulture);
        if (string.CompareOrdinal(text, at, digits, 0, digits.Length) != 0) return null;

        int end = at + digits.Length;
        if (end < text.Length && text[end] is '.' or ')') end++;
        while (end < text.Length && text[end] == ' ') end++;

        if (end == at + digits.Length || end == text.Length) return null;
        return char.IsUpper(text[end]) ? end - at : null;
    }

    /// <summary>The last thing before this place ends a sentence, spaces aside.</summary>
    private static bool AfterASentence(string text, int at)
    {
        int before = at - 1;
        while (before >= 0 && text[before] == ' ') before--;
        return before >= 0 && text[before] is '.' or '!' or '?' or ')' or ':' or '"' or '\'' or '”' or '’';
    }

    /// <summary>Paragraphs a page lost the break between: <c>to 180 C.Thinly slice</c>. A decimal stays.</summary>
    private static IEnumerable<string> Glued(string text)
    {
        int start = 0;
        for (int at = 1; at + 2 < text.Length; at++)
        {
            if (text[at] != '.' || !char.IsUpper(text[at + 1]) || !char.IsLower(text[at + 2])) continue;
            if (!char.IsLetterOrDigit(text[at - 1]) && text[at - 1] != ')') continue;

            yield return text[start..(at + 1)];
            start = at + 1;
        }

        yield return text[start..];
    }

    /// <summary>What's still long, cut at the first sentence end once a part holds enough.</summary>
    private static IEnumerable<string> Sentences(string text)
    {
        if (text.Length <= LongStep)
        {
            yield return text;
            yield break;
        }

        int start = 0;
        for (int at = 0; at + 2 < text.Length; at++)
        {
            if (at + 1 - start < EnoughForAPart || text[at] is not ('.' or '!' or '?')) continue;
            if (text[at + 1] != ' ' || !char.IsUpper(text[at + 2])) continue;

            yield return text[start..(at + 1)];
            start = at + 2;
        }

        yield return text[start..];
    }
}
