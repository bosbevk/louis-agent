namespace LouisAgent.Core.Tools.Web.Quotes;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>The rules VerifyQuote matches a quote by (F30, FX16; S, CL15, from WebTools): no model, no network.</summary>
internal static partial class QuoteMatch
{
    /// <summary>How far after a product's name its price may stand: a name, a size, a badge, then the price.</summary>
    internal const int ProductWindowChars = 300;

    /// <summary>A body this large with less readable text than <see cref="ShellTextChars"/> is a shell: the product is in its scripts.</summary>
    private const int ShellBodyBytes = 20_000;

    /// <summary>Less readable text than this on a large page: a menu and a footer, not a product page.</summary>
    private const int ShellTextChars = 300;

    /// <summary>
    /// Text as a quote is compared: entities decoded, every run of whitespace (a non-breaking space too) one space, lower
    /// case, and a rand sign joined to its amount (<c>r 35.99</c> is <c>r35.99</c>: a price drawn in two tags, FX16).
    /// </summary>
    internal static string Normalise(string text) =>
        RandSpace().Replace(AnyWhitespace().Replace(WebUtility.HtmlDecode(text).Replace(' ', ' '), " ").Trim().ToLowerInvariant(), "r");

    /// <summary>
    /// A quote that is only a price (FX16): without its prices and the words around a price (<c>each</c>, <c>now</c>,
    /// <c>per kg</c>, <c>was</c>, <c>save</c>) fewer than three letters are left, so it names no product.
    /// </summary>
    internal static bool IsOnlyAPrice(string normalised) =>
        PriceWord().Replace(Price().Replace(normalised, " "), " ").Count(char.IsLetter) < 3;

    /// <summary>
    /// The quote stands for <paramref name="product"/> (FX16): it starts within <see cref="ProductWindowChars"/> after
    /// one of the product's names on the page with no other price between, or it holds the name itself. On a list of
    /// products the next price after a name is that product's; any later one is another's. A special's old price or
    /// saving (<c>was R29.99</c>, <c>save R7.00</c>) between is skipped, and is never the product's price itself.
    /// </summary>
    internal static bool StandsAfter(string text, string product, string quote)
    {
        if (quote.Contains(product, StringComparison.Ordinal)) return text.Contains(quote, StringComparison.Ordinal);
        for (int at = text.IndexOf(product, StringComparison.Ordinal); at >= 0; at = text.IndexOf(product, at + 1, StringComparison.Ordinal))
        {
            int start = at + product.Length;
            string window = text.Substring(start, Math.Min(ProductWindowChars, text.Length - start));
            int found = window.IndexOf(quote, StringComparison.Ordinal);
            if (found < 0) continue;
            string between = window[..found];
            if (!AfterWasOrSave().IsMatch(between) && !Price().IsMatch(OldPrice().Replace(between, " "))) return true;
        }
        return false;
    }

    /// <summary>
    /// A page a JavaScript app draws later (F30 hand check: Pick n Pay's product pages): no text at all, or a large body
    /// whose readable text is only a menu, a "loading" line and a footer. Its "Not found" would be false, so it is
    /// rendered instead.
    /// </summary>
    internal static bool LooksLikeShell(string readable, string all, int bodyLength) =>
        Normalise(all).Length < 3 || bodyLength > ShellBodyBytes && Normalise(readable).Length < ShellTextChars;

    // A quote copied across a line break still matches.
    [GeneratedRegex(@"\s+")]
    private static partial Regex AnyWhitespace();

    // A price in rand as normalised text reads it (r22.99, r 1 299,00, r35).
    private const string Rand = @"r ?\d+(?:[ ,]\d{3})*(?:[.,]\d{2})?";

    [GeneratedRegex(@"\b" + Rand)]
    private static partial Regex Price();

    // The space between a rand sign and its amount.
    [GeneratedRegex(@"\br (?=\d)")]
    private static partial Regex RandSpace();

    // A special's old price or saving, which is no product's price (was r29.99, save r7.00).
    [GeneratedRegex(@"\b(?:was|save|normally)\s*:?\s*" + Rand)]
    private static partial Regex OldPrice();

    // The quote itself follows "was" or "save": the old price or the saving, not the price.
    [GeneratedRegex(@"\b(?:was|save|normally)\s*:?\s*$")]
    private static partial Regex AfterWasOrSave();

    // Words that come with a price and name no product.
    [GeneratedRegex(@"\b(?:each|ea|now|only|per|from|special|promo|price|was|save|normally|kg|g|ml|l|pack)\b")]
    private static partial Regex PriceWord();
}
