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

    /// <summary>The recipe as it came, or with the page's words in place of a yield of one.</summary>
    public static Recipe Checked(Recipe recipe, string html, Uri page) =>
        SaysOneServing(recipe.Yield) ? recipe with { Yield = FromWords(html, page) } : recipe;

    /// <summary>Exactly one serving, or a bare 1: nothing else is doubted.</summary>
    private static bool SaysOneServing(string? yield) =>
        yield?.Trim().ToLowerInvariant() is "1" or "1 serving" or "1 servings";

    /// <summary>What the page says: its marked-up number first, then the first line of text that gives one.</summary>
    private static string? FromWords(string html, Uri page) =>
        Marked(html) ?? HtmlText.ToText(html, page).Text.Split('\n').Select(line => Yield(line.Trim()))
            .FirstOrDefault(yield => yield is not null);

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
    private static string? Yield(string line)
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
