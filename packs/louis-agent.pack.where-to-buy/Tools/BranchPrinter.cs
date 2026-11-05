using LouisAgent.Composition.Messages;
using System.Globalization;
using System.Text;
using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Messages;

namespace LouisAgent.Packs.WhereToBuy.Tools;

/// <summary>
/// Writes a branch lookup for the model, framed as data since the map's names are what its mappers typed: the town's
/// municipality and province, a row per branch nearest first, and what to say when none is the town's own.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public static class BranchPrinter
{
    /// <summary>A shop's branches, and what to say when none is the town's own.</summary>
    public static string Print(BranchSearch search, string town)
    {
        string name = search.Town.Name ?? town;
        return Framed(search, town, search.Branches.Count == 0
                ? WhereToBuyToolMessage.NoBranch
                : WhereToBuyToolMessage.BranchesFound,
            WhereToBuyToolMessage.BranchesNote,
            new
            {
                search.Shop,
                Km = search.SearchedKm,
                Town = name,
                search.Branches.Count
            });
    }

    /// <summary>The supermarkets near the town, of any chain.</summary>
    public static string PrintShops(BranchSearch search, string town)
    {
        string name = search.Town.Name ?? town;
        return Framed(search, town, search.Branches.Count == 0
                ? WhereToBuyToolMessage.NoShops
                : WhereToBuyToolMessage.ShopsFound,
            WhereToBuyToolMessage.ShopsNote,
            new
            {
                Km = search.SearchedKm,
                Town = name,
                search.Branches.Count
            });
    }

    /// <summary>The frame, the town's province, the opening line, and any rows with their note.</summary>
    private static string Framed(BranchSearch search, string town, WhereToBuyToolMessage opening,
        WhereToBuyToolMessage note, object values)
    {
        string area = search.Town.Address?.Area ?? town;
        string province = search.Town.Address?.State ?? "South Africa";
        var text = new StringBuilder();
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.UntrustedFrame));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TownFound,
            new
            {
                Town = search.Town.Name ?? town,
                Area = area,
                Province = province
            }));
        text.AppendLine();
        text.AppendLine(MessageText.Format(opening, values));
        if (search.Branches.Count > 0)
        {
            text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TableHeader, new { Area = area }));
            text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TableRule));
            foreach (var branch in search.Branches) text.AppendLine(Row(branch));
            text.AppendLine();
            text.AppendLine(MessageText.Format(note, values));
        }

        text.Append(MessageText.Format(WhereToBuyToolMessage.FrameEnd));
        return text.ToString();
    }

    /// <summary>One branch's row; what the map leaves out is a dash.</summary>
    private static string Row(Branch branch) => MessageText.Format(WhereToBuyToolMessage.BranchRow, new
    {
        Name = Cell(branch.Name),
        Street = Cell(branch.Street),
        Place = Cell(branch.Place),
        Km = branch.Km.ToString("0.0", CultureInfo.InvariantCulture),
        InArea = MessageText.Format(branch.InTownsArea ? WhereToBuyToolMessage.Yes : WhereToBuyToolMessage.No),
    });

    /// <summary>A table cell: a dash for nothing, and no pipe that would break the row.</summary>
    private static string Cell(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Replace('|', '/').Trim();
}
