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
/// clauses (ADR 0051). The shapes come from a check of all our sites' real pages. A page that gives nothing here goes
/// on to <see cref="PlainRunRecipe"/>, and if that gives nothing too it's refused, and never read again. Asking a model
/// would reach more pages, at a cost on every read and with mistakes nothing here could see.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md">ADR 0082: the recipe library is an open feed</seealso>
internal static class PageTextRecipe
{
    private const string Serves = "Serves ";
    private const string Makes = "Makes ";
    private const string Feeds = "Feeds ";
    private static readonly string[] Portions = [" porsies", " servings"];

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
        string[] lines = LinesOf(words);

        // A heading with another heading straight under it is a tab's label, with nothing of its own.
        int ingredientsAt = Enumerable.Range(0, lines.Length).Where(at => IsIngredientsHeading(lines[at]) &&
                !(at + 1 < lines.Length && (IsMethodHeading(lines[at + 1]) || IsIngredientsHeading(lines[at + 1]))))
            .DefaultIfEmpty(-1).First();
        if (ingredientsAt < 0) return null;

        // With no method heading, "Step 1" starts the method, and it's a step's part, so it stays.
        int headingAt = Array.FindIndex(lines, ingredientsAt, IsMethodHeading);
        int methodAt = headingAt >= 0 ? headingAt : Array.FindIndex(lines, ingredientsAt, IsStepPart);
        if (methodAt < 0) return null;

        if (Name(lines[..ingredientsAt], title) is not { } name) return null;
        if (Ingredients(lines[ingredientsAt..methodAt]) is not { Count: >= FewestIngredients } ingredients) return null;

        var parts = ingredients.Select(line => line.Section).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var steps = Steps(lines[(headingAt >= 0 ? methodAt + 1 : methodAt)..], parts);
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

            // A heading too long to name a part is the page talking.
            if (line.StartsWith('#') && Words(line).Length > LongestHeading) continue;

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
    /// skipped, as is a sentence straight under a part, and a line before the first item is the first step. Anything
    /// else could be anything, and guessing could keep half a recipe as if it were whole, so that gives nothing. So
    /// does a "Serves" among plain steps: a second recipe whose lines we can't tell from the first's.
    /// </summary>
    public static List<RecipeStep>? Steps(string[] lines, HashSet<string> parts)
    {
        string[] own = NumberedParts(OwnLines(lines, parts));
        bool listed = own.Any(line => IsStepItem(line, 0));
        bool inParts = !listed && own.Any(IsStepPart);
        var steps = new List<RecipeStep>();
        string? part = null;
        int counted = 0;
        for (int at = 0; at < own.Length; at++)
        {
            string line = own[at];
            if (!line.Any(char.IsLetterOrDigit)) continue;

            if (line.StartsWith('#'))
            {
                part = Words(line);
                continue;
            }

            if (!listed && YieldOf(line) is not null) return null;

            // A last line ending in a colon promises something that isn't there, such as a video.
            if (line.EndsWith(':') && at + 1 == own.Length) break;

            if (IsItem(line) && line.EndsWith(':'))
            {
                part = Words(WithoutMarker(line));
                continue;
            }

            // Sentences before "Step 1" only open the method, and among plain steps a cookie notice is no step. Among
            // listed steps it stays what it was: a line that could be anything, so the page stays out.
            if (inParts && part is null && !IsStepPart(line)) continue;
            if (!listed && IsCookieNotice(line)) continue;

            if (!listed && (IsLabel(line) || IsStepPart(line)))
            {
                part = Words(line);
                continue;
            }

            if (listed && !IsStepItem(line, counted))
            {
                if (!own.Skip(at + 1).Any(later => IsStepItem(later, counted))) return [.. steps, .. LastPart(own, at)];

                if (at > 0 && IsStepPart(own[at - 1])) continue;

                if (IsPartLine(line) || IsStepPart(line) || LooksLikeAName(line))
                {
                    part = Words(line);
                    continue;
                }

                if (line.EndsWith(':') || (at > 0 && IsPartLine(own[at - 1]))) continue;
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
    /// A method whose numbered lines name parts, with plain sentences under each: "1. Shape and fry" over "Fry them.".
    /// Those lines become "Step 1: Shape and fry". A numbered line that's a sentence is a step itself, so the method
    /// stays as it is.
    /// </summary>
    private static string[] NumberedParts(string[] own)
    {
        int[] numbered = [.. Enumerable.Range(0, own.Length).Where(at => WrittenNumber(own[at]) is not null)];
        bool theyAreParts = numbered.Length >= 2 && numbered.All(at =>
            !WithoutMarker(own[at]).EndsWith('.') && at + 1 < own.Length && !IsItem(own[at + 1]));
        if (!theyAreParts) return own;

        return [.. own.Select(line => WrittenNumber(line) is { } number ? $"Step {number}: {WithoutMarker(WithoutBullet(line))}" : line)];
    }

    /// <summary>A last numbered part with one sentence under it, "Step 5: serve", has that step to give.</summary>
    private static RecipeStep[] LastPart(string[] own, int at) =>
        IsStepPart(own[at]) && at + 1 < own.Length ? [new RecipeStep(Words(own[at]), WithoutMarker(own[at + 1]))] : [];

    /// <summary>
    /// The lines that can be this recipe's steps: up to a heading that names none of its parts, a link, a credit, a
    /// lone dash starting the page's sharing buttons, a piece of the page's own HTML, a cook's aside, or a second
    /// recipe's ingredients, whose name stays out too.
    /// </summary>
    public static string[] OwnLines(string[] lines, HashSet<string> parts)
    {
        string[] own = [.. DashesJoined(lines).Where(line => !IsReadAlso(line))];
        int end = Array.FindIndex(own, line => (line.StartsWith('#') && !parts.Contains(Words(line))) || line == "-" ||
                                               line.StartsWith('<') || HasLink(line) || line.Contains('©') ||
                                               IsAside(line) || IsIngredientsHeading(line));
        if (end < 0) return own;

        bool namesTheNext = end > 0 && IsIngredientsHeading(own[end]) && own[end - 1].Length <= LongestHeading &&
                            !own[end - 1].EndsWith('.');
        return own[..(namesTheNext ? end - 1 : end)];
    }

    /// <summary>
    /// A list that starts with a lone dash has each item's dash on the line above it, as Jan Braai's can: then a dash
    /// starts a step, and joins the line under it. Anywhere else a lone dash is the sharing buttons.
    /// </summary>
    private static List<string> DashesJoined(string[] lines)
    {
        if (lines is not ["-", ..]) return [.. lines];

        var joined = new List<string>();
        for (int at = 0; at < lines.Length; at++)
        {
            bool dash = lines[at] == "-" && at + 1 < lines.Length;
            joined.Add(dash ? $"- {lines[++at]}" : lines[at]);
        }

        return joined;
    }

    /// <summary>The first heading before the ingredients, else the page's title without the site's name.</summary>
    public static string? Name(string[] before, string? title)
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
    public static string? YieldOf(string line)
    {
        string words = InBrackets(WithoutBullet(line)).Replace("Servings:", "Serves:", StringComparison.Ordinal)
            .Replace("Serves: ", Serves, StringComparison.Ordinal).Replace("Serves:", Serves, StringComparison.Ordinal)
            .Replace(Feeds, Serves, StringComparison.Ordinal).Replace("Yields: ", Makes, StringComparison.Ordinal).Trim();
        string? portions = Portions.FirstOrDefault(word => words.EndsWith(word, StringComparison.OrdinalIgnoreCase));
        if (portions is not null && char.IsAsciiDigit(words[0])) words = Serves + words[..^portions.Length];

        // "Serves 20 vetkoekies" counts things, not people.
        if (words.StartsWith(Serves, StringComparison.Ordinal) && char.IsLetter(words[^1]))
            words = Makes + words[Serves.Length..];

        return PageYield.Yield(words);
    }

    /// <summary>A yield alone in brackets: "what you need (makes 6)". Any other line as it came.</summary>
    private static string InBrackets(string line)
    {
        int open = line.IndexOf('(');
        int close = line.IndexOf(')', Math.Max(open, 0));
        if (open < 0 || close < 0) return line;

        string inside = line[(open + 1)..close];
        bool says = new[] { Serves, Makes, Feeds }.Any(word => inside.StartsWith(word, StringComparison.OrdinalIgnoreCase));
        return says ? char.ToUpperInvariant(inside[0]) + inside[1..] : line;
    }
}
