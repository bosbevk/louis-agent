using System.Text.RegularExpressions;
using LouisAgent.Evals.Markdown;

namespace LouisAgent.Packs.Shops.Checks.Reading;

/// <summary>
/// Tells you whether a reply names a branch of the user's shop under Where to buy, and if not, why not. It reads what a
/// person reads: the heading may be a bold line, or have an emoji or a number before its words.
/// </summary>
/// <remarks>
/// The section is the shop table when it has one (a row of the shop whose branch or street cell holds a place counts),
/// and the prose otherwise (<c>Pick n Pay Menlyn</c>, <c>PnP in Paarl Mall</c>, <c>Pick n Pay — Brooklyn Mall</c>). A
/// shop with no place, another shop's branch, a "nearest" branch with no place and a branch named outside the section
/// are not a branch; the finding says which of them it was.
/// </remarks>
public static partial class BranchReading
{
    /// <summary>What the reply says about the shop's branch: named, or which way it fell short.</summary>
    public static BranchFinding Of(string? answer, string shop)
    {
        if (Section(answer) is not { } section) return BranchFinding.NoSection;
        var names = NamesOf(shop);

        // A table with a shop column and a branch column decides, when the section has one.
        bool sawTable = false, nearest = false;
        foreach (var table in Lists.Tables(section))
        {
            var header = Lists.Cells(table[0]);
            int shopColumn = header.FindIndex(cell => ShopHeader().IsMatch(cell)),
                branchColumn = header.FindIndex(cell => BranchHeader().IsMatch(cell));
            if (shopColumn < 0 || branchColumn < 0) continue;
            sawTable = true;
            var placeColumns = header.Select((cell, index) => (cell, index))
                .Where(c => c.index == branchColumn || StreetHeader().IsMatch(c.cell)).Select(c => c.index).ToList();
            foreach (var row in table.Skip(1).Where(row => !Lists.IsSeparator(row)).Select(Lists.Cells))
            {
                if (shopColumn >= row.Count || !names.IsMatch(row[shopColumn])) continue;
                var cells = placeColumns.Where(index => index < row.Count).Select(index => row[index]).ToList();
                if (cells.Any(cell => HoldsAPlace(cell, names))) return BranchFinding.Named;
                nearest |= cells.Any(cell => Nearest().IsMatch(cell));
            }
        }

        if (sawTable) return nearest ? BranchFinding.OnlyNearest : BranchFinding.NoBranchOfShop;

        string text = InlineLinkText().Replace(section, " ").Replace("*", "");
        foreach (Match name in names.Matches(text))
            if (PlaceAfter(text[(name.Index + name.Length)..].Split('\n')[0]) is not null)
                return BranchFinding.Named;
        return Nearest().IsMatch(text) ? BranchFinding.OnlyNearest : BranchFinding.NoBranchOfShop;
    }

    /// <summary>The shop's names as a pattern (<c>Pick n Pay</c> and <c>PnP</c> are one shop).</summary>
    private static Regex NamesOf(string shop)
    {
        string host = ShopSites.Hosts.First(known => known.Name == shop).Host;
        string names = string.Join("|",
            ShopSites.Hosts.Where(known => known.Host == host).Select(known => Regex.Escape(known.Name)));
        return new Regex($@"\b(?:{names})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// The words between a shop's name and its place that name no place ("PnP in Paarl", "Pick n Pay se tak in
    /// Malmesbury"), and the ones that mean no place yet.
    /// </summary>
    private static readonly HashSet<string> NoPlaceWords =
    [
        "in", "te", "by", "at", "near", "naby", "se", "tak", "branch", "op", "website", "shop", "winkel", "nearest",
        "naaste", "closest"
    ];

    /// <summary>
    /// The place that follows a shop's name on a line: its first word that isn't filler, if it's capitalised and isn't
    /// a shop's name.
    /// </summary>
    private static string? PlaceAfter(string rest)
    {
        string? place = SplitAtPlace().Split(rest)
            .FirstOrDefault(token => token.Length > 0 && !NoPlaceWords.Contains(token.ToLowerInvariant()));
        return place is { Length: > 0 } && char.IsUpper(place[0]) && !ShopSites.Hosts.Any(known =>
            known.Name.StartsWith(place, StringComparison.OrdinalIgnoreCase))
            ? place
            : null;
    }

    /// <summary>
    /// Whether a table cell holds a place: a capitalised word that is neither the shop's name, filler, nor another
    /// shop's name.
    /// </summary>
    private static bool HoldsAPlace(string cell, Regex names)
    {
        string text = names.Replace(InlineLinkText().Replace(cell, " ").Replace("*", ""), " ");
        return SplitAtPlace().Split(text).Any(token =>
            token.Length > 0 && char.IsUpper(token[0]) && !NoPlaceWords.Contains(token.ToLowerInvariant())
            && !ShopSites.Hosts.Any(known => known.Name.StartsWith(token, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// The lines under the Where to buy heading (<c>Waar om te koop</c>) up to the next heading or bold line; nothing
    /// if there is no such heading.
    /// </summary>
    private static string? Section(string? answer)
    {
        var lines = (answer ?? "").Split('\n');
        int start = Array.FindIndex(lines, line => Heading().IsMatch(line));
        return start < 0
            ? null
            : string.Join('\n', lines.Skip(start + 1).TakeWhile(line => !EndsSection().IsMatch(line)));
    }

    /// <summary>
    /// The Where to buy heading: a heading or a bold line, with anything that is no letter (an emoji, a number, a dot)
    /// before its words.
    /// </summary>
    [GeneratedRegex(@"^\s*(?:#{1,6}|\*\*)[^\p{L}\n]*(?:where to buy|waar om te koop)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Heading();

    /// <summary>The next heading or bold line, which ends the section.</summary>
    [GeneratedRegex(@"^\s*(?:#{1,6}\s|\*\*[^*\n]+\*\*\s*:?\s*$)")]
    private static partial Regex EndsSection();

    /// <summary>A markdown link or a bare web address, taken out so its words aren't read as a place.</summary>
    [GeneratedRegex(@"\[[^\]]*\]\([^)]*\)|https?://\S+")]
    private static partial Regex InlineLinkText();

    /// <summary>What splits a line into words: spaces, table bars, punctuation and dashes.</summary>
    [GeneratedRegex(@"[\s|,:;()\-–—.]+")]
    private static partial Regex SplitAtPlace();

    /// <summary>A "nearest" branch, in English or Afrikaans.</summary>
    [GeneratedRegex(@"\b(?:nearest|closest|naaste)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Nearest();

    /// <summary>A table's shop column heading.</summary>
    [GeneratedRegex(@"^\W*(?:shop|winkel|store)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ShopHeader();

    /// <summary>A table's branch column heading.</summary>
    [GeneratedRegex(@"^\W*(?:branch|tak)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BranchHeader();

    /// <summary>A table column that holds a place as well: a street, a mall, an address or an area.</summary>
    [GeneratedRegex(@"\b(?:street|mall|address|straat|adres|location|area|sentrum)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StreetHeader();
}
