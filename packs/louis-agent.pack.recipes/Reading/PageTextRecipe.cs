using LouisAgent.Packs.Recipes.Reading.Photos;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Finds the recipe on a page with no recipe data, when the page lays it out plainly: an ingredients heading, the
/// lines under it, a method heading and the steps. Anything less plain gives nothing, because what we keep is served
/// for good, and a recipe read wrongly once would be wrong every time.
/// </summary>
/// <remarks>
/// A small reader over plain string methods, not a regular expression (ADR 0057), walking the page's lines with guard
/// clauses (ADR 0051). The shapes come from real pages: Zola Nene, Sarah Graham, FinGlobal, Rhodes and Vrouekeur. It
/// only decides what the library keeps; the cook's first read of such a page stays the page's text. Asking a model
/// would reach more pages, at a cost on every read and with mistakes nothing here could see.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md">ADR 0082: the recipe library is an open feed</seealso>
internal static class PageTextRecipe
{
    private static readonly string[] IngredientHeadings = ["ingredients", "bestanddele"];
    private static readonly string[] IngredientPartHeadings = ["ingredients for ", "bestanddele vir "];

    private static readonly string[] MethodHeadings =
    [
        "method", "instructions", "directions", "preparation", "what to do", "steps", "metode", "werkwyse", "werkswyse",
        "aanwysings", "instruksies"
    ];

    private static readonly string[] PartStarts = ["for ", "vir "];
    private static readonly string[] Bullets = ["- ", "* ", "• "];
    private const string Link = "](";
    private const string Serves = "Serves ";
    private const string Makes = "Makes ";
    private const string Portions = " porsies";

    /// <summary>A heading is short; a sentence that happens to start the same way isn't one.</summary>
    private const int LongestHeading = 40;

    /// <summary>A line longer than this is a story about the ingredients, not one of them.</summary>
    private const int LongestIngredient = 200;

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

        int ingredientsAt = Array.FindIndex(lines, IsIngredientsHeading);
        if (ingredientsAt < 0) return null;

        int methodAt = Array.FindIndex(lines, ingredientsAt, IsMethodHeading);
        if (methodAt < 0) return null;

        if (Name(lines[..ingredientsAt], title) is not { } name) return null;
        if (Ingredients(lines[ingredientsAt..methodAt]) is not { Count: >= FewestIngredients } ingredients) return null;

        var steps = Steps(lines[(methodAt + 1)..]);
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
        foreach (string line in lines)
        {
            if (IsIngredientsHeading(line))
            {
                part = PartNamedIn(line);
                continue;
            }

            if (line.Contains(Link, StringComparison.Ordinal)) return null;

            if (line.StartsWith('#') || (!IsItem(line) && IsPartLine(line)))
            {
                part = Words(line);
                continue;
            }

            if (listed && !IsItem(line)) continue;
            if (YieldOf(line) is not null) continue;

            string text = WithoutMarker(line);
            if (text.Length > LongestIngredient) return null;

            kept.Add(new RecipeIngredient(part, text));
        }

        return kept;
    }

    /// <summary>
    /// The steps, up to whatever comes after them: a heading or a link always ends them. In a list, a short line just
    /// before more items names a part, and any other line ends the list, which is how a page's cookie notice and
    /// adverts stay out. A long line with more items after it could be anything, and stopping there would keep half a
    /// recipe as if it were whole, so that gives nothing.
    /// </summary>
    private static List<RecipeStep>? Steps(string[] lines)
    {
        string[] own =
        [
            .. lines.TakeWhile(line => !line.StartsWith('#') && !line.Contains(Link, StringComparison.Ordinal))
        ];
        bool listed = own.Any(line => IsStepItem(line, 0));
        var steps = new List<RecipeStep>();
        string? part = null;
        int counted = 0;
        for (int at = 0; at < own.Length; at++)
        {
            string line = own[at];
            if (listed && !IsStepItem(line, counted))
            {
                if (!own.Skip(at + 1).Any(later => IsStepItem(later, counted))) break;
                if (line.Length > LongestHeading) return null;

                part = Words(line);
                continue;
            }

            // Only a number that counts the list comes off, so "10 minutes before baking" keeps its own.
            string text = WithoutBullet(line);
            if (CountedNumberLength(text, counted) is var length and > 0)
            {
                text = text[length..].TrimStart();
                counted++;
            }

            steps.Add(new RecipeStep(part, text));
        }

        return steps;
    }

    /// <summary>The first heading before the ingredients, else the page's title without the site's name.</summary>
    private static string? Name(string[] before, string? title)
    {
        string? heading = before
            .Where(line => line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            .Select(Words)
            .FirstOrDefault(words => words.Length > 0);
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

    /// <summary>Whether the line is an ingredients heading, alone or naming a part.</summary>
    private static bool IsIngredientsHeading(string line)
    {
        string words = Words(line).ToLowerInvariant();
        return words.Length <= LongestHeading &&
               (IngredientHeadings.Contains(words) ||
                IngredientPartHeadings.Any(start => words.StartsWith(start, StringComparison.Ordinal)));
    }

    /// <summary>Whether the line is a method heading.</summary>
    private static bool IsMethodHeading(string line) => MethodHeadings.Contains(Words(line).ToLowerInvariant());

    /// <summary>The part an ingredients heading names: "syrup" in "Ingredients for syrup".</summary>
    private static string? PartNamedIn(string line)
    {
        string words = Words(line);
        string? start = IngredientPartHeadings.FirstOrDefault(found =>
            words.StartsWith(found, StringComparison.OrdinalIgnoreCase));
        return start is null ? null : words[start.Length..];
    }

    /// <summary>Whether a plain line names a part: "For the sauce", "Vir die sous".</summary>
    private static bool IsPartLine(string line)
    {
        string words = Words(line);
        return words.Length <= LongestHeading &&
               PartStarts.Any(start => words.StartsWith(start, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Whether the line is a list item: a bullet, or a number such as "1." but not "1 onion".</summary>
    private static bool IsItem(string line) =>
        Bullets.Any(bullet => line.StartsWith(bullet, StringComparison.Ordinal)) || WrittenNumberLength(line) > 0;

    /// <summary>Whether the line is the next item of a list of steps, where a bare "2" counts when it's next.</summary>
    private static bool IsStepItem(string line, int counted) => IsItem(line) || CountedNumberLength(line, counted) > 0;

    /// <summary>The line without its bullet or its written number.</summary>
    private static string WithoutMarker(string line)
    {
        string text = WithoutBullet(line);
        return text[WrittenNumberLength(text)..].TrimStart();
    }

    /// <summary>The line without its bullet.</summary>
    private static string WithoutBullet(string line) =>
        Bullets.Any(bullet => line.StartsWith(bullet, StringComparison.Ordinal)) ? line[2..].TrimStart() : line;

    /// <summary>How long a written number such as "12." or "3)" is at the line's start, or 0.</summary>
    private static int WrittenNumberLength(string line)
    {
        int digits = line.TakeWhile(char.IsAsciiDigit).Count();
        return digits > 0 && line.Length > digits + 1 && line[digits] is '.' or ')' && line[digits + 1] == ' '
            ? digits + 1
            : 0;
    }

    /// <summary>How long the number at the line's start is, if it's next in the count, written or bare, or 0.</summary>
    private static int CountedNumberLength(string line, int counted)
    {
        string next = (counted + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!line.StartsWith(next, StringComparison.Ordinal) || line.Length <= next.Length) return 0;

        return line[next.Length] switch
        {
            ' ' => next.Length,
            '.' or ')' when line.Length > next.Length + 1 && line[next.Length + 1] == ' ' => next.Length + 1,
            _ => 0
        };
    }

    /// <summary>A line's words, without a heading's marks and bold, or a closing colon.</summary>
    private static string Words(string line) => line.TrimStart('#', '*', ' ').TrimEnd('*', ':', ' ');
}
