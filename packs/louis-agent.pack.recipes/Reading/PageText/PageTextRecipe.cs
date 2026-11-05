using LouisAgent.Packs.Recipes.Reading.Photos;
using LouisAgent.Packs.Recipes.Recipes;
using static LouisAgent.Packs.Recipes.Reading.PageText.PageTextLines;

namespace LouisAgent.Packs.Recipes.Reading.PageText;

/// <summary>
/// Finds the recipe on a page with no recipe data, when the page lays it out plainly: an ingredients heading, the
/// lines under it, a method heading and the steps. Anything less plain gives nothing, because what we keep is served
/// for good, and a recipe read wrongly once would be wrong every time.
/// </summary>
/// <remarks>
/// A small reader over plain string methods, not a regular expression (ADR 0057), walking the page's lines with guard
/// clauses (ADR 0051). The shapes come from a check of all our sites' real pages. A page that gives nothing here is
/// refused, and never read again. Asking a model would reach more pages, at a cost on every read and with mistakes
/// nothing here could see.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md">ADR 0082: the recipe library is an open feed</seealso>
internal static class PageTextRecipe
{
    private const string Serves = "Serves ";
    private const string Makes = "Makes ";
    private const string Portions = " porsies";

    /// <summary>A line longer than this is a story about the ingredients, not one of them.</summary>
    private const int LongestIngredient = 300;

    /// <summary>A first step can run to a sentence or two before the list starts; longer is the page talking.</summary>
    private const int LongestFirstLine = 200;

    /// <summary>More than one ingredient: one line under the heading is rarely a recipe.</summary>
    private const int FewestIngredients = 2;

    /// <summary>More steps than this means the page ran on past the recipe.</summary>
    private const int MostSteps = 40;

    /// <summary>The recipe on this page, when it's plainly there. It has no photo: the caller checks those.</summary>
    /// <remarks>Guard clauses, one per way a page can fall short (ADR 0051).</remarks>
    public static Recipe? Read(string html, Uri page)
    {
        var (title, words) = HtmlText.ToText(html, page);
        string[] lines =
        [
            .. PrintedRecipeText.Cut(PagePhotos.WithoutImages(words)).Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
        ];

        // A heading with another heading straight under it is a tab's label, with nothing of its own.
        int ingredientsAt = Enumerable.Range(0, lines.Length).Where(at => IsIngredientsHeading(lines[at]) &&
                !(at + 1 < lines.Length && (IsMethodHeading(lines[at + 1]) || IsIngredientsHeading(lines[at + 1]))))
            .DefaultIfEmpty(-1).First();
        if (ingredientsAt < 0) return null;

        int methodAt = Array.FindIndex(lines, ingredientsAt, IsMethodHeading);
        if (methodAt < 0) return null;

        if (Name(lines[..ingredientsAt], title) is not { } name) return null;
        if (Ingredients(lines[ingredientsAt..methodAt]) is not { Count: >= FewestIngredients } ingredients) return null;

        var parts = ingredients.Select(line => line.Section).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var steps = Steps(lines[(methodAt + 1)..], parts);
        if (steps is not { Count: > 0 and <= MostSteps }) return null;

        return new Recipe(name, YieldIn(lines[..methodAt]), null, null, null, ingredients, steps, null, null, page);
    }

    /// <summary>The ingredients, each under its part, or nothing if a line shows this isn't a plain list.</summary>
    private static List<RecipeIngredient>? Ingredients(string[] lines)
    {
        // In a list, what's between the items isn't an ingredient, such as Sarah Graham's times. With no list, every
        // line is one.
        bool listed = lines.Any(IsItem);
        var kept = new List<RecipeIngredient>();
        string? part = null;
        foreach (string line in lines.Where(line => !IsReadAlso(line)))
        {
            if (IsIngredientsHeading(line))
            {
                part = PartNamedIn(line);
                continue;
            }

            if ((!IsItem(line) && HasLink(line) && !listed) || IsAnInstruction(line)) return null;

            if (line.StartsWith('#') || (!IsItem(line) && IsPartLine(line)) || (IsItem(line) && line.EndsWith(':')))
            {
                part = Words(WithoutMarker(line));
                continue;
            }

            if (listed && !IsItem(line)) continue;
            if (YieldOf(line) is not null) continue;

            string text = WithoutLinks(WithoutMarker(line));
            if (text.Length > LongestIngredient) return null;

            kept.Add(new RecipeIngredient(part, text));
        }

        return kept;
    }

    /// <summary>
    /// The steps, up to whatever comes after them. In a list, the last line that isn't an item ends it, which is how a
    /// page's cookie notice and adverts stay out. With more items to come, a name is a part, an opening sentence is
    /// skipped, and a line before the first item is the first step. Anything else could be anything, and guessing could
    /// keep half a recipe as if it were whole, so that gives nothing. So does a "Serves" among plain steps: a second
    /// recipe whose lines we can't tell from the first's.
    /// </summary>
    private static List<RecipeStep>? Steps(string[] lines, HashSet<string> parts)
    {
        string[] own = OwnLines(lines, parts);
        bool listed = own.Any(line => IsStepItem(line, 0));
        var steps = new List<RecipeStep>();
        string? part = null;
        int counted = 0;
        for (int at = 0; at < own.Length; at++)
        {
            string line = own[at];
            if (line.StartsWith('#'))
            {
                part = Words(line);
                continue;
            }

            if (!listed && YieldOf(line) is not null) return null;

            if (IsItem(line) && line.EndsWith(':'))
            {
                part = Words(WithoutMarker(line));
                continue;
            }

            if (!listed && IsLabel(line))
            {
                part = Words(line);
                continue;
            }

            if (listed && !IsStepItem(line, counted))
            {
                if (!own.Skip(at + 1).Any(later => IsStepItem(later, counted))) break;

                if (IsPartLine(line) || LooksLikeAName(line))
                {
                    part = Words(line);
                    continue;
                }

                if (line.EndsWith(':')) continue;
                if (steps.Count > 0 || line.Length > LongestFirstLine) return null;
            }

            // A written "1." always comes off, as a count can start again in each part. A bare number comes off only if
            // it's next, so "10 minutes before baking" keeps its own.
            string text = WithoutMarker(line);
            if (CountedNumberLength(WithoutBullet(line), counted) is var length and > 0)
            {
                text = WithoutBullet(line)[length..].TrimStart();
                counted++;
            }

            steps.Add(new RecipeStep(part, text));
        }

        return steps;
    }

    /// <summary>
    /// The lines that can be this recipe's steps: up to a heading that names none of its parts, a link, a credit, a
    /// lone dash starting the page's sharing buttons, or a second recipe's ingredients, whose name stays out too.
    /// </summary>
    private static string[] OwnLines(string[] lines, HashSet<string> parts)
    {
        string[] own = [.. lines.Where(line => !IsReadAlso(line))];
        int end = Array.FindIndex(own, line => (line.StartsWith('#') && !parts.Contains(Words(line))) || line == "-" ||
                                               HasLink(line) || line.Contains('©') || IsIngredientsHeading(line));
        if (end < 0) return own;

        bool namesTheNext = end > 0 && IsIngredientsHeading(own[end]) && own[end - 1].Length <= LongestHeading &&
                            !own[end - 1].EndsWith('.');
        return own[..(namesTheNext ? end - 1 : end)];
    }

    /// <summary>The first heading before the ingredients, else the page's title without the site's name.</summary>
    private static string? Name(string[] before, string? title)
    {
        string? heading = before
            .Where(line => line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            .Select(Words)
            .FirstOrDefault(words => words.Length > 0 && !NamesNoDish(words));
        if (heading is not null) return heading;

        string? name = title?.Split(" | ")[0].Split(" - ")[0].Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>What the recipe makes, from the first line before the method that says so.</summary>
    private static string? YieldIn(string[] lines) => lines.Select(YieldOf).FirstOrDefault(found => found is not null);

    /// <summary>One line as a yield, in the ways these pages write it.</summary>
    private static string? YieldOf(string line)
    {
        string words = WithoutBullet(line).Replace("Serves:", Serves, StringComparison.Ordinal).Trim();
        if (words.EndsWith(Portions, StringComparison.Ordinal) && char.IsAsciiDigit(words[0]))
            words = Serves + words[..^Portions.Length];

        // "Serves 20 vetkoekies" counts things, not people.
        if (words.StartsWith(Serves, StringComparison.Ordinal) && char.IsLetter(words[^1]))
            words = Makes + words[Serves.Length..];

        return PageYield.Yield(words);
    }
}
