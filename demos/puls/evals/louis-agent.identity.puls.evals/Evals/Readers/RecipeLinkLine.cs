using LouisAgent.Evals.Readers.Kinds;

namespace LouisAgent.Identities.Puls.Evals.Readers;

/// <summary>
/// Reads the recipe's link from the line the cook writes for it: <c>Skakel: {url}</c> in Afrikaans, <c>Link: {url}</c>
/// in English, the print's own link. The first such line with a web address on it is the answer's; a later one (under
/// Where to buy, say), the word <i>link</i> inside a sentence, an image and a price link are not.
/// </summary>
/// <remarks>
/// A Strategy (Gang of Four, ADR 0059) of the link kind: the recipes pack's check knows what makes a link from a
/// source, and how Puls's cook writes the line is Puls's knowledge, so it lives here, beside the other readers. The
/// eval file names it <c>recipe-link</c>. Read with string methods, never a regex (ADR 0057). It copes with how a
/// model dresses the line, which the recorded transcripts show and a trimmed fixture would hide: the address alone, a
/// markdown link, <c>**Link:**</c> or <c>**Link**:</c>, an address in angle brackets, a list dash and a full stop
/// after the address.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0059-an-identity-ships-readers-not-checks.md">ADR 0059: an identity ships readers, not checks</seealso>
public sealed class RecipeLinkLine : ILinkReader
{
    /// <summary>The words that open the line: Afrikaans, then English.</summary>
    private static readonly string[] Labels = ["Skakel", "Link"];

    /// <summary>
    /// What a model puts before a label (a list dash, a quote, bold or italic marks) and may put after it.
    /// </summary>
    private static readonly char[] Marks = ['-', '*', '_', '>', ' ', '\t'];

    /// <summary>What ends a sentence or closes a mark, and is not part of an address that precedes it.</summary>
    private static readonly char[] Trailing = ['.', ',', ';', ':', '!', '?', '*', '_', '\'', '"'];

    /// <inheritdoc />
    public string Name => "recipe-link";

    /// <inheritdoc />
    public string? RecipeLink(string answer) =>
        answer.Split('\n').Select(AddressOnLine).FirstOrDefault(address => address is not null);

    /// <summary>
    /// The web address on a line that opens with the label, or null: the line has another opening, or nothing after
    /// the label is an address.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private static string? AddressOnLine(string line)
    {
        string text = line.Trim().TrimStart(Marks);
        string? label = Labels.FirstOrDefault(word => text.StartsWith(word, StringComparison.OrdinalIgnoreCase));
        if (label is null) return null;

        // A bold label's closing marks can sit before or after the colon.
        string afterLabel = text[label.Length..].TrimStart('*', '_');
        if (!afterLabel.StartsWith(':')) return null;

        return Address(afterLabel[1..].TrimStart(Marks));
    }

    /// <summary>
    /// The address a value is: a markdown link's, one in angle brackets, or the first word; trailing punctuation
    /// off. An image, a link with a picture in it and a path with no host give none.
    /// </summary>
    private static string? Address(string value)
    {
        if (value.StartsWith('!') || value.StartsWith("[!", StringComparison.Ordinal)) return null;

        string word = value.StartsWith('[') ? InParentheses(value) : value.Split(' ', '\t', '\r')[0];
        string address = Clean(word.Trim('<', '>'));
        bool isWebAddress = address.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                            || address.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        return isWebAddress ? address : null;
    }

    /// <summary>The address of a markdown link, <c>[text](address "title")</c>; empty when it isn't one.</summary>
    private static string InParentheses(string link)
    {
        int open = link.IndexOf("](", StringComparison.Ordinal);
        int close = open < 0 ? -1 : link.IndexOf(')', open);
        return close < 0 ? "" : link[(open + 2)..close].Split(' ', '\t')[0];
    }

    /// <summary>
    /// The address without the punctuation after it; a closing bracket stays only when the address opens one.
    /// </summary>
    private static string Clean(string address)
    {
        string trimmed = address.TrimEnd(Trailing);
        return trimmed.EndsWith(')') && !trimmed.Contains('(') ? Clean(trimmed[..^1]) : trimmed;
    }
}
