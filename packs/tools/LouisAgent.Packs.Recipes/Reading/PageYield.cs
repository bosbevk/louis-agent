using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Catches a recipe that says it serves one when it plainly doesn't. Taste's data says <c>1 serving</c> on a bobotie
/// with 1 kg of mince while its page says <c>Serves 6 to 8</c>, and a cook scaling by the data would buy four times too
/// much. When the data says one, the page's own words decide, and with no words the yield is unknown.
/// </summary>
/// <remarks>
/// The one exception to reading a page's data before its text (ADR 0067): a yield of exactly one serving looks like a
/// template's default, not something a person wrote. <c>1 cake</c> or <c>1 loaf</c> stays, since those are real. It's
/// a plain line reader, not a regex (ADR 0057), and it lives in code because a prompt rule can't see that the two
/// disagree (ADR 0080).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0067-a-pages-own-data-before-its-text.md">ADR 0067: a page's own data before its text</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static class PageYield
{
    private const string Serves = "Serves ";
    private const string Makes = "Makes ";
    private const string Mark = "itemprop=\"recipeYield\"";

    /// <summary>Longer than this is a sentence that happens to start the same way, not a yield.</summary>
    private const int LongestLine = 40;

    /// <summary>Longer than this before "servings" isn't a number of them.</summary>
    private const int LongestNumber = 10;

    /// <summary>Words a page puts before the number of people it feeds.</summary>
    private static readonly string[] PeopleWords = ["serves", "servings", "serving", "feeds", "bedien", "dien", "porsies"];

    /// <summary>Words a page puts before what it makes.</summary>
    private static readonly string[] MakesWords = ["makes", "yields", "yield"];

    /// <summary>What can follow a number of people, which the yield doesn't need to say again.</summary>
    private static readonly string[] PeopleEndings = [" persons", " people", " mense", " porsies", " servings", " serving"];

    /// <summary>The recipe as it came, or with the page's words where its yield is missing or says one.</summary>
    public static Recipe Checked(Recipe recipe, string html, Uri page) =>
        string.IsNullOrWhiteSpace(recipe.Yield) || SaysOneServing(recipe.Yield)
            ? recipe with { Yield = FromWords(html, page) }
            : recipe;

    /// <summary>Exactly one serving, or a bare 1: nothing else is doubted.</summary>
    private static bool SaysOneServing(string? yield) =>
        yield?.Trim().ToLowerInvariant() is "1" or "1 serving" or "1 servings";

    /// <summary>What the page says: its marked-up number first, then the first line of text that gives one.</summary>
    private static string? FromWords(string html, Uri page)
    {
        if (Marked(html) is { } marked) return marked;

        string[] lines =
        [
            .. HtmlText.ToText(html, page).Text.Replace(' ', ' ').Split('\n').Select(line => line.Trim('#', '*', ' '))
                .Where(line => line.Length > 0)
        ];
        for (int at = 0; at < lines.Length; at++)
        {
            // Some pages put the word and its number on lines of their own.
            bool alone = !lines[at].TrimEnd(':').Contains(' ') && at + 1 < lines.Length;
            if (InWords(lines[at]) is { } found) return found;
            if (alone && InWords($"{lines[at].TrimEnd(':')} {lines[at + 1]}") is { } split) return split;
        }

        return null;
    }

    /// <summary>
    /// One line as a yield, however the page words it: <c>Serves: 6</c>, <c>SERVES 4 to 6 persons</c>, <c>Yield: 12
    /// rolls</c>, <c>8 servings</c>, or any of those in a bracket, or after a dish's name in capitals.
    /// </summary>
    /// <remarks>Guard clauses, one per way a line can say it (ADR 0051).</remarks>
    private static string? InWords(string line)
    {
        if (Plainly(line) is { } said) return said;
        if (AfterADishsName(line) is { } named) return named;

        int open = line.IndexOf('(');
        if (open < 0) return null;

        int close = line.IndexOf(')', open);
        return Plainly(close < 0 ? line[(open + 1)..] : line[(open + 1)..close]);
    }

    /// <summary>The yield after a dish's name in capitals: <c>BOBOTIE Serves 4 to 6</c>. A sentence's isn't.</summary>
    private static string? AfterADishsName(string line)
    {
        int at = line.IndexOf(" Serves ", StringComparison.Ordinal);
        if (at <= 0) return null;

        var letters = line[..at].Where(char.IsLetter).ToList();
        return letters.Count > 0 && letters.All(char.IsUpper) ? Plainly(line[(at + 1)..]) : null;
    }

    /// <summary>A line that starts by saying the yield, with nothing before it.</summary>
    /// <remarks>Guard clauses, one per way a line can say it (ADR 0051).</remarks>
    private static string? Plainly(string line)
    {
        string text = line.Trim();
        if (text.Length == 0) return null;

        if (char.IsAsciiDigit(text[0])) return PeopleCounted(text);

        int space = text.IndexOf(' ');
        if (space < 0) return null;

        string word = text[..space].TrimEnd(':');
        string rest = text[space..].Trim();
        if (rest.Length == 0 || rest.Length > LongestLine || !char.IsAsciiDigit(rest[0])) return null;

        if (MakesWords.Contains(word, StringComparer.OrdinalIgnoreCase)) return rest;
        return PeopleWords.Contains(word, StringComparer.OrdinalIgnoreCase) ? Servings(rest) : null;
    }

    /// <summary>A line that starts with a number of servings: <c>8-10 servings, more if a relish</c>.</summary>
    private static string? PeopleCounted(string text)
    {
        string? ending = PeopleEndings.FirstOrDefault(found => text.Contains(found, StringComparison.OrdinalIgnoreCase));
        if (ending is null) return null;

        string number = text[..text.IndexOf(ending, StringComparison.OrdinalIgnoreCase)];
        return number.Length <= LongestNumber && !number.Any(char.IsLetter) ? Servings(number) : null;
    }

    /// <summary>A number of people as the yield says it, without a word for people after it.</summary>
    private static string Servings(string people)
    {
        string? ending = PeopleEndings.FirstOrDefault(found => people.EndsWith(found, StringComparison.OrdinalIgnoreCase));
        return MessageText.Format(RecipeToolMessage.RecipeServings,
            new { People = ending is null ? people : people[..^ending.Length] });
    }

    /// <summary>The element marked <c>itemprop="recipeYield"</c>, read as a line: a bare number is people.</summary>
    private static string? Marked(string html)
    {
        int mark = html.IndexOf(Mark, StringComparison.Ordinal);
        if (mark < 0) return null;

        int start = html.IndexOf('>', mark) + 1;
        int end = html.IndexOf('<', start);
        if (start == 0 || end < 0) return null;

        string value = html[start..end].Trim();
        return Follows(value, Makes) ? value[Makes.Length..] : Yield(Serves + value);
    }

    /// <summary>One line as a yield, when it's short and a number follows the word.</summary>
    /// <remarks>Guard clauses, one per case (ADR 0051).</remarks>
    public static string? Yield(string line)
    {
        if (line.Length > LongestLine) return null;
        if (Follows(line, Serves))
            return MessageText.Format(RecipeToolMessage.RecipeServings, new { People = line[Serves.Length..] });
        return Follows(line, Makes) ? line[Makes.Length..] : null;
    }

    /// <summary>The word starts the line and a digit comes straight after it.</summary>
    private static bool Follows(string line, string word) =>
        line.StartsWith(word, StringComparison.Ordinal) && line.Length > word.Length && char.IsAsciiDigit(line[word.Length]);
}
