namespace LouisAgent.Packs.ShopPrices.Prices;

/// <summary>
/// Turns a product's name into the words we compare: lower case, letters and digits only, each word in its singular
/// form, so "Onions - fresh", "onion" and "ONIONS" meet on "onion".
/// </summary>
/// <remarks>
/// A plain scan, no pattern (ADR 0057). The singular is a rough English one ("tomatoes" to "tomato", "cherries" to
/// "cherry", "eggs" to "egg"); both sides go through it, so a word it gets wrong is still the same word on both.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public static class ProductWords
{
    /// <summary>The words of a name, in order.</summary>
    public static IReadOnlyList<string> Of(string? name)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        // One past the end, so the last word is kept too.
        foreach (char c in (name ?? "") + " ")
        {
            if (char.IsLetterOrDigit(c))
            {
                word.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (word.Length > 0) words.Add(Singular(word.ToString()));
            word.Clear();
        }

        return words;
    }

    /// <summary>One word in its singular form, roughly: enough to make plurals meet.</summary>
    /// <remarks>Guard clauses, the longest ending first (ADR 0051).</remarks>
    private static string Singular(string word)
    {
        if (word.Length <= 3) return word;
        if (word.EndsWith("ies", StringComparison.Ordinal)) return word[..^3] + "y";
        if (word.EndsWith("oes", StringComparison.Ordinal)) return word[..^2];
        if (word.EndsWith("ss", StringComparison.Ordinal)) return word;
        return word.EndsWith('s') ? word[..^1] : word;
    }
}
