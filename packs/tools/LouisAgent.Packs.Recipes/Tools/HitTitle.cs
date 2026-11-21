namespace LouisAgent.Packs.Recipes.Tools;

/// <summary>
/// A hit's title as the cook sees it in a search's list, without the country. Every site in our table is South African,
/// so a title saying so tells the cook nothing about the dish, yet the prompt asks for South African food first and it
/// read those pages about twice as often as chance. Only the list changes: the page, the print and what's stored keep
/// the title as the site wrote it.
/// </summary>
/// <remarks>
/// In code and not in the prompt, because a rule asking the model to ignore words it can read is weaker than not
/// showing them (ADR 0080). Plain string methods, not a regular expression (ADR 0057).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static class HitTitle
{
    /// <summary>The ways a title says the country, the longest of a kind first so the whole of it goes.</summary>
    private static readonly string[] TheCountry =
    [
        "Proudly South African", "in South Africa", "from South Africa", "South Africa's", "South Africa’s", "South African",
        "South Africa",
        "Suid-Afrikaanse", "Suid-Afrikaans", "Suid-Afrika"
    ];

    /// <summary>What a title can be left with at either end once the country is out.</summary>
    private static readonly char[] LeftOver = [' ', ':', '-', '–', '—', '·', ',', '|'];

    /// <summary>The title without the country, or as it was if that leaves nothing to call it.</summary>
    public static string Shown(string title)
    {
        string shown = TheCountry.Aggregate(title, (text, words) => text.Replace(words, "", StringComparison.OrdinalIgnoreCase));
        if (shown.Length == title.Length) return title;

        string tidied = string.Join(' ', shown.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim(LeftOver);
        return tidied.Length > 0 ? tidied : title;
    }
}
