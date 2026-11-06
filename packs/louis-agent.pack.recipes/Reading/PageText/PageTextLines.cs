using System.Globalization;
using LouisAgent.Packs.Recipes.Reading.Photos;

namespace LouisAgent.Packs.Recipes.Reading.PageText;

/// <summary>
/// Says what one line of a recipe page is: a heading, a list item, a part's name, an instruction, or a pointer to
/// another page. The rules come from real pages, so each one here has a site behind it in the tests.
/// </summary>
/// <remarks>
/// Plain string methods, not regular expressions (ADR 0057). Apart from <see cref="PageTextRecipe"/> because it answers
/// one question about one line, and the walk through the page is a different job that changes for different reasons.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
internal static class PageTextLines
{
    private static readonly string[] IngredientHeadings = ["ingredients", "bestanddele", "ingredients needed", "what you need"];
    private static readonly string[] IngredientPartHeadings = ["ingredients for ", "bestanddele vir ", "what you need for "];
    private static readonly string[] IngredientPhrases = ["ingredients needed ", "ingredients: ", "what you need "];

    private static readonly string[] MethodHeadings =
    [
        "method", "instructions", "directions", "preparation", "what to do", "steps", "metode", "werkwyse", "werkswyse",
        "aanwysings", "instruksies", "cooking instructions", "how to make it", "what you need to do"
    ];

    /// <summary>Words that make a marked heading a method: "What are the steps for this recipe?".</summary>
    private static readonly string[] MethodWords = ["method", "steps", "instructions", "directions", "metode"];

    private static readonly string[] PartStarts = ["for ", "vir ", "step ", "stap "];
    private static readonly string[] ReadAlso = ["lees ook", "read also", "also read", "read more"];

    /// <summary>How a cook's aside starts, in these exact capitals: "Tip the batter in" is a step.</summary>
    private static readonly string[] Asides = ["TIP ", "TIP:", "Tip:", "NOTE:", "Note:", "NOTES:", "Notes:"];

    /// <summary>Words that start an instruction: with no amount on the line, it's a step in the wrong list.</summary>
    private static readonly string[] Instructions =
    [
        "mix", "stir", "add", "combine", "blend", "whisk", "season", "heat", "cook", "bake", "fry", "boil", "serve",
        "preheat", "pre-heat", "meng", "roer", "voeg", "klits", "kook", "bak", "verhit", "voorverhit", "bedien", "stel"
    ];

    /// <summary>Headings that name a section of a site and no dish, with any closing "s" taken off.</summary>
    private static readonly string[] SiteSections = ["recipe", "resep", "resepte"];

    private static readonly string[] Bullets = ["- ", "* ", "• "];

    /// <summary>What can follow a list's written number: "1. ", "1) " or "1.) ".</summary>
    private static readonly string[] NumberMarks = [". ", ") ", ".) "];

    private const string Link = "](";

    /// <summary>A heading is short; a sentence that happens to start the same way isn't one.</summary>
    public const int LongestHeading = 40;

    /// <summary>The page's words as lines to read: no images, no blank lines, nothing after the recipe.</summary>
    public static string[] LinesOf(string words) =>
    [
        .. PrintedRecipeText.Cut(PagePhotos.WithoutImages(words)).Replace(' ', ' ').Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
    ];

    /// <summary>Whether the line holds a link.</summary>
    public static bool HasLink(string line) => line.Contains(Link, StringComparison.Ordinal);

    /// <summary>Whether the line is an ingredients heading, alone, naming a part, or as a phrase.</summary>
    public static bool IsIngredientsHeading(string line)
    {
        string words = Words(line).ToLowerInvariant();
        return words.Length <= LongestHeading && !MethodHeadings.Contains(words) &&
               (IngredientHeadings.Contains(words) ||
                IngredientPartHeadings.Concat(IngredientPhrases).Any(start => words.StartsWith(start, StringComparison.Ordinal)));
    }

    /// <summary>Whether the line is a method heading, by its words or, for a marked heading, a word in it.</summary>
    public static bool IsMethodHeading(string line)
    {
        string words = Words(line).ToLowerInvariant();
        return MethodHeadings.Contains(words) ||
               (line.StartsWith('#') && MethodWords.Any(word => words.Split(' ').Contains(word)));
    }

    /// <summary>The part an ingredients heading names: "syrup" in "Ingredients for syrup".</summary>
    public static string? PartNamedIn(string line)
    {
        string words = Words(line);
        string? start = IngredientPartHeadings.FirstOrDefault(found =>
            words.StartsWith(found, StringComparison.OrdinalIgnoreCase));
        return start is null ? null : words[start.Length..];
    }

    /// <summary>
    /// Whether a plain line names a part: "For the sauce", "Step 1: the stew", "EIERBOLAAG", "Deeg:". A long line
    /// ending in a colon is a list's opening sentence instead.
    /// </summary>
    public static bool IsPartLine(string line)
    {
        string words = Words(line);
        bool capitals = words.Any(char.IsLetter) && words.Where(char.IsLetter).All(char.IsUpper);
        return words.Length <= LongestHeading &&
               (capitals || line.EndsWith(':') ||
                PartStarts.Any(start => words.StartsWith(start, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>The part a line opens with: "For the filling" in "For the filling: (from my book)".</summary>
    public static string? PartBeforeColon(string line)
    {
        int colon = line.IndexOf(':');
        bool opens = colon is > 0 and <= LongestHeading &&
                     PartStarts.Any(start => line.StartsWith(start, StringComparison.OrdinalIgnoreCase));
        return opens ? line[..colon] : null;
    }

    /// <summary>Whether a line names one numbered part of a method: "Step 1: the dough".</summary>
    public static bool IsStepPart(string line) =>
        IsPartLine(line) && (Words(line).StartsWith("step ", StringComparison.OrdinalIgnoreCase) ||
                             Words(line).StartsWith("stap ", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether a plain step is really a label: short, and in capitals or ending in a colon.</summary>
    public static bool IsLabel(string line)
    {
        string words = Words(line);
        return words.Length <= LongestHeading &&
               (line.EndsWith(':') || (words.Any(char.IsLetter) && words.Where(char.IsLetter).All(char.IsUpper)));
    }

    /// <summary>Whether a line among steps is only a name: short, no full stop, no amount, no instruction.</summary>
    public static bool LooksLikeAName(string line) =>
        line.Length <= LongestHeading && !line.EndsWith('.') && !line.Any(char.IsAsciiDigit) && !StartsLikeAnInstruction(line);

    /// <summary>Whether a heading is a section of the site and no dish: "Recipes".</summary>
    public static bool NamesNoDish(string words) =>
        SiteSections.Contains(words.TrimEnd('s', 'S'), StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a line points at another page: "LEES OOK: …".</summary>
    public static bool IsReadAlso(string line) =>
        ReadAlso.Any(start => Words(line).StartsWith(start, StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether a line is the cook's aside, not a step.</summary>
    public static bool IsAside(string line) => Asides.Any(start => line.StartsWith(start, StringComparison.Ordinal));

    /// <summary>Whether a line among the ingredients is an instruction: it starts like one, with no amount.</summary>
    public static bool IsAnInstruction(string line) =>
        StartsLikeAnInstruction(line) && !WithoutMarker(line).Any(char.IsAsciiDigit);

    /// <summary>Whether the line is a list item: a bullet, or a number such as "1." but not "1 onion".</summary>
    public static bool IsItem(string line) =>
        Bullets.Any(bullet => line.StartsWith(bullet, StringComparison.Ordinal)) || WrittenNumberLength(line) > 0;

    /// <summary>Whether the line is the next item of a list of steps, where a bare "2" counts when it's next.</summary>
    public static bool IsStepItem(string line, int counted) => IsItem(line) || CountedNumberLength(line, counted) > 0;

    /// <summary>The line without its bullet or its written number.</summary>
    public static string WithoutMarker(string line)
    {
        string text = WithoutBullet(line);
        return text[WrittenNumberLength(text)..].TrimStart();
    }

    /// <summary>The line without its bullet.</summary>
    public static string WithoutBullet(string line) =>
        Bullets.Any(bullet => line.StartsWith(bullet, StringComparison.Ordinal)) ? line[2..].TrimStart() : line;

    /// <summary>The line with each link as its words: an ingredient may link to another recipe.</summary>
    public static string WithoutLinks(string line)
    {
        string text = line;
        while (text.IndexOf(Link, StringComparison.Ordinal) is var middle and >= 0)
        {
            int open = text.LastIndexOf('[', middle);
            int close = text.IndexOf(')', middle);
            if (open < 0 || close < 0) return text;

            text = text[..open] + text[(open + 1)..middle] + text[(close + 1)..];
        }

        return text;
    }

    /// <summary>How long the number at the line's start is, if it's next in the count, written or bare, or 0.</summary>
    public static int CountedNumberLength(string line, int counted)
    {
        string next = (counted + 1).ToString(CultureInfo.InvariantCulture);
        if (!line.StartsWith(next, StringComparison.Ordinal) || line.Length <= next.Length) return 0;

        return line[next.Length] switch
        {
            ' ' => next.Length,
            '.' or ')' when line.Length > next.Length + 1 && line[next.Length + 1] == ' ' => next.Length + 1,
            _ => 0
        };
    }

    /// <summary>A line's words, without a heading's marks and bold, or a closing colon.</summary>
    public static string Words(string line) => line.TrimStart('#', '*', ' ').TrimEnd('*', ':', ' ');

    /// <summary>Whether the line's first word is one that starts an instruction.</summary>
    public static bool StartsLikeAnInstruction(string line) =>
        Instructions.Contains(WithoutMarker(line).Split(' ', 2)[0].TrimEnd(',', ':'), StringComparer.OrdinalIgnoreCase);

    /// <summary>How long a written number such as "12.", "3)" or "1.)" is at the line's start, or 0.</summary>
    private static int WrittenNumberLength(string line)
    {
        int digits = line.TakeWhile(char.IsAsciiDigit).Count();
        if (digits == 0) return 0;

        string? mark = NumberMarks.FirstOrDefault(found => line.AsSpan(digits).StartsWith(found, StringComparison.Ordinal));
        return mark is null ? 0 : digits + mark.Length - 1;
    }
}
