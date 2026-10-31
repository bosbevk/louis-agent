namespace LouisAgent.Packs.Shops.Checks.Reading;

/// <summary>
/// The lines a shop rule looks for in an answer: that its prices are estimates, that the shop has no branch in the
/// town, and that it names the shop's branch under Where to buy.
/// </summary>
/// <remarks>
/// Moved from Puls's <c>Shops</c> with <c>estimates-at-shop</c>, the rule that reads them (F49.3 step 3). Read by plain
/// scans, no pattern (ADR 0057), pinned by <c>ShopLinesTests</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public static class ShopLines
{
    /// <summary>
    /// The answer says its prices are estimates, not checked at a shop, in either language: the line the cook writes
    /// under its list when the user chose no shop.
    /// </summary>
    public static bool EstimatesLine(string? answer) =>
        EstimatesWords.Any(words => (answer ?? "").Contains(words, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The answer says the shop has no branch in the town ("no PnP branch", "geen PnP-tak"): "no" or "geen", then
    /// "branch" or "tak", as whole words, at most forty characters apart, with no full stop, line break or table pipe
    /// between them.
    /// </summary>
    public static bool NoBranchLine(string? answer)
    {
        var text = answer ?? "";
        var words = WordsOf(text);
        for (var i = 0; i < words.Count; i++)
        {
            if (!IsOneOf(words[i].Text, NoWords)) continue;
            // Read on through the words still in the same phrase; past the first one that isn't, none is.
            for (var j = i + 1; j < words.Count && OnePhrase(text, words[i], words[j]); j++)
                if (IsOneOf(words[j].Text, BranchWords))
                    return true;
        }

        return false;
    }

    /// <summary>
    /// Whether a later word is in the same phrase as a first: at most forty characters after it, with no full stop,
    /// line break or table pipe between them.
    /// </summary>
    private static bool OnePhrase(string text, Word first, Word later) =>
        later.Start - first.End <= NoBranchGap &&
        text.AsSpan(first.End, later.Start - first.End).IndexOfAny(".\n|") < 0;

    /// <summary>Whether a word is one of these, case ignored.</summary>
    private static bool IsOneOf(string word, string[] words) => words.Contains(word, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The answer names the shop's branch under Where to buy, in either language (<see cref="BranchReading"/> says
    /// which case it is when it doesn't).
    /// </summary>
    public static bool NamesBranch(string? answer, string shop) =>
        BranchReading.Of(answer, shop) == BranchFinding.Named;

    /// <summary>The estimates line's words, in English and Afrikaans.</summary>
    private static readonly string[] EstimatesWords = ["Prices are estimates", "Pryse is skattings"];

    /// <summary>The word that says there is none, in English and Afrikaans.</summary>
    private static readonly string[] NoWords = ["no", "geen"];

    /// <summary>The word for a shop's branch, in English and Afrikaans.</summary>
    private static readonly string[] BranchWords = ["branch", "tak"];

    /// <summary>The most characters between "no" and "branch" that still read as one phrase.</summary>
    private const int NoBranchGap = 40;

    /// <summary>One word of a text, and where it starts and ends.</summary>
    private readonly record struct Word(string Text, int Start, int End);

    /// <summary>
    /// The text's words in order: the runs of letters, digits and underscores, so "PnP-tak" is "PnP" and "tak".
    /// </summary>
    private static List<Word> WordsOf(string text)
    {
        var words = new List<Word>();
        var start = -1;
        // One past the end, so a word the text ends on is read too.
        for (var i = 0; i <= text.Length; i++)
        {
            var inWord = i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_');
            if (inWord && start < 0) start = i;
            if (inWord || start < 0) continue;

            words.Add(new Word(text[start..i], start, i));
            start = -1;
        }

        return words;
    }
}
