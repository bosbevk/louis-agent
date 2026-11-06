using LouisAgent.Packs.Recipes.Recipes;
using static LouisAgent.Packs.Recipes.Reading.PageText.PageTextLines;

namespace LouisAgent.Packs.Recipes.Reading.PageText;

/// <summary>
/// Finds the recipe on a page with no headings to go by: a run of short lines, most starting with an amount, then
/// sentences. Five sites in our table lay their recipes out this way. It only says yes to the plain shape, because what
/// we keep is served for good: enough lines, mostly amounts, and straight after them at least two steps, or one long
/// paragraph that holds the whole method.
/// </summary>
/// <remarks>
/// The last link of a Chain of Responsibility: the page's data, then <see cref="PageTextRecipe"/>'s headings, then
/// this, each trying only what the one before couldn't. A separate reader keeps the headings reader as strict as it
/// was, and one that goes by how lines look can be judged on its own. Plain string methods, not a regular expression
/// (ADR 0057). What it can't see: an ingredient with no amount at the very top of the run stays out, as nothing tells
/// it from the page's own words above it, and a part named by more than one word stays a line of the list, because
/// treating such a line as a part would lose an ingredient like "Olive oil" without a word.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md">ADR 0082: the recipe library is an open feed</seealso>
internal static class PlainRunRecipe
{
    /// <summary>Longer than this is a sentence, not an ingredient.</summary>
    private const int LongestLine = 120;

    /// <summary>Fewer lines than this could be anything: a short list in a story.</summary>
    private const int FewestLines = 4;

    /// <summary>One step could be a sentence that happens to follow a list; two is a method.</summary>
    private const int FewestSteps = 2;

    /// <summary>How long a lone paragraph must be to count as a whole method.</summary>
    private const int ShortestWholeMethod = 300;

    /// <summary>Fewer sentences than this in a long paragraph is the page talking, not a method.</summary>
    private const int FewestSentencesInAWholeMethod = 3;

    /// <summary>More steps than this means the page ran on past the recipe.</summary>
    private const int MostSteps = 40;

    /// <summary>How far above the run a yield is looked for.</summary>
    private const int YieldLinesAbove = 4;

    /// <summary>The fractions a line can start with. Most lines in a run start with one of these or a digit.</summary>
    private const string Fractions = "½¼¾⅓⅔⅛⅜⅝⅞";

    /// <summary>The recipe on this page, when it's plainly there. It has no photo: the caller checks those.</summary>
    public static Recipe? Read(string html, Uri page)
    {
        var (title, words) = HtmlText.ToText(html, page);
        string[] lines = LinesOf(words);

        int start = FirstRun(lines);
        if (start < 0) return null;
        if (PageTextRecipe.Name(lines[..start], title) is not { } name) return null;

        var ingredients = new List<RecipeIngredient>();
        var steps = new List<RecipeStep>();
        string? part = PartAbove(lines, start);

        // Most pages have one run then their steps, but a part can have its own method straight under its lines.
        for (int at = start; at < lines.Length; part = null)
        {
            at = ReadRun(lines, at, part, ingredients);

            // A run with no heading can still have one over its method, which isn't a step.
            if (at < lines.Length && IsMethodHeading(lines[at])) at++;

            int end = NextRun(lines, at);
            var parts = ingredients.Select(line => line.Section).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
            string[] between = lines[at..end];

            if (PageTextRecipe.Steps(between, parts) is not { } found) return null;
            steps.AddRange(found);

            // Something ended the steps before the next run, so that run isn't this recipe's.
            if (PageTextRecipe.OwnLines(between, parts).Length < between.Length) break;
            at = end;
        }

        bool hasAMethod = steps.Count is >= FewestSteps and <= MostSteps || (steps is [var only] && IsAWholeMethod(only.Text));
        if (!hasAMethod) return null;

        return new Recipe(name, YieldAbove(lines, start), null, null, null, ingredients, steps, null, null, page);
    }

    /// <summary>Whether one paragraph is long enough, and has sentences enough, to be the whole method.</summary>
    private static bool IsAWholeMethod(string text) =>
        text.Length >= ShortestWholeMethod &&
        text.Split(". ", StringSplitOptions.RemoveEmptyEntries).Length >= FewestSentencesInAWholeMethod;

    /// <summary>Where the first run starts: enough lines, mostly amounts, with a step straight after them.</summary>
    private static int FirstRun(string[] lines)
    {
        for (int at = 0; at < lines.Length; at++)
        {
            // A line above a heading isn't a list's first: that's how "12 glasses" sits over its recipe's first part.
            if (!IsIngredient(lines[at]) || !StartsWithAmount(lines[at])) continue;
            if (at + 1 < lines.Length && lines[at + 1].StartsWith('#')) continue;

            var run = new List<RecipeIngredient>();
            int end = ReadRun(lines, at, null, run);
            bool mostlyAmounts = run.Count(line => StartsWithAmount(line.Text)) * 5 >= run.Count * 3;
            if (run.Count >= FewestLines && mostlyAmounts && end < lines.Length && StartsAStep(lines[end])) return at;
        }

        return -1;
    }

    /// <summary>Adds a run's lines, each under its part, and says where the run stopped.</summary>
    private static int ReadRun(string[] lines, int from, string? firstPart, List<RecipeIngredient> into)
    {
        string? part = firstPart;
        int at = from;
        for (; at < lines.Length; at++)
        {
            string line = lines[at];
            if (PartAt(lines, at) is { } named)
            {
                part = named;
                continue;
            }

            if (!IsIngredient(line)) break;
            into.Add(new RecipeIngredient(part, WithoutLinks(WithoutMarker(line))));
        }

        return at;
    }

    /// <summary>Where the next run starts, a part with lines of its own under it, or the end of the page.</summary>
    private static int NextRun(string[] lines, int from)
    {
        for (int at = from; at + 2 < lines.Length; at++)
        {
            bool opensARun = PartAt(lines, at) is not null && StartsWithAmount(lines[at + 1]) && IsIngredient(lines[at + 2]);
            if (opensARun) return at;
        }

        return lines.Length;
    }

    /// <summary>The part named straight above a run, if that line names one.</summary>
    private static string? PartAbove(string[] lines, int start) => start == 0 ? null : PartAt(lines, start - 1);

    /// <summary>
    /// The part this line names, when ingredients follow it: a heading, a line ending in a colon, or one capitalised
    /// word over a line with an amount ("Syrup").
    /// </summary>
    /// <remarks>Guard clauses, one per way a line can name a part (ADR 0051).</remarks>
    private static string? PartAt(string[] lines, int at)
    {
        string line = lines[at];
        if (at + 1 >= lines.Length || !IsIngredient(lines[at + 1])) return null;
        if (IsIngredientsHeading(line)) return PartNamedIn(line);
        if (line.StartsWith('#')) return Words(line);
        if (IsItem(line)) return line.EndsWith(':') ? Words(WithoutMarker(line)) : null;
        if (PartBeforeColon(line) is { } opened) return opened;
        if (IsPartLine(line)) return Words(line);

        bool oneWord = !line.Contains(' ') && char.IsUpper(line[0]) && !StartsWithAmount(line);
        return oneWord && StartsWithAmount(lines[at + 1]) ? line : null;
    }

    /// <summary>Whether a line reads as an ingredient: short, no sentence, no instruction, nothing else.</summary>
    private static bool IsIngredient(string line)
    {
        string text = WithoutLinks(WithoutMarker(line));
        bool isSomethingElse = line.StartsWith('#') || line.StartsWith('<') || IsPartLine(line) || IsIngredientsHeading(line) ||
                               (HasLink(line) && !IsItem(line) && !StartsWithAmount(line)) ||
                               PageTextRecipe.YieldOf(line) is not null;
        return text.Length is > 0 and <= LongestLine && !isSomethingElse && !EndsASentence(text) &&
               !StartsLikeAnInstruction(line);
    }

    /// <summary>Whether a line could start the method: a sentence, an instruction, a part or its heading.</summary>
    private static bool StartsAStep(string line) =>
        WithoutMarker(line).Length > LongestLine || EndsASentence(WithoutMarker(line)) || StartsLikeAnInstruction(line) ||
        IsStepPart(line) || IsMethodHeading(line);

    /// <summary>Whether a line ends the way a sentence does.</summary>
    private static bool EndsASentence(string text) => text.EndsWith('.') || text.EndsWith('!');

    /// <summary>Whether a line starts with a number or a fraction, once any bullet is off.</summary>
    private static bool StartsWithAmount(string line)
    {
        string text = WithoutBullet(line);
        return text.Length > 0 && (char.IsAsciiDigit(text[0]) || Fractions.Contains(text[0]));
    }

    /// <summary>What the recipe makes, from just above the run: in words, or a bare count over a heading.</summary>
    private static string? YieldAbove(string[] lines, int start)
    {
        for (int at = Math.Max(0, start - YieldLinesAbove); at < start; at++)
        {
            if (PageTextRecipe.YieldOf(lines[at]) is { } said) return said;
            if (IsBareCount(lines[at]) && lines[at + 1].StartsWith('#')) return lines[at];
        }

        return null;
    }

    /// <summary>Whether a line is only a number and what it counts: "12 glasies".</summary>
    private static bool IsBareCount(string line) =>
        StartsWithAmount(line) && line.Split(' ') is [_, var thing] && thing.All(char.IsLetter);
}
