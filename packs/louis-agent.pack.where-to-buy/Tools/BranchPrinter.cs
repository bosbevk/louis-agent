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
    /// <summary>The whole result.</summary>
    public static string Print(BranchSearch search, string town)
    {
        string area = search.Town.Address?.Area ?? town;
        string province = search.Town.Address?.State ?? "South Africa";
        var text = new StringBuilder();
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.UntrustedFrame));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TownFound,
            new { Town = search.Town.Name ?? town, Area = area, Province = province }));
        text.AppendLine();
        if (search.Branches.Count == 0)
        {
            text.AppendLine(MessageText.Format(WhereToBuyToolMessage.NoBranch,
                new { search.Shop, Km = search.SearchedKm, Town = search.Town.Name ?? town }));
            text.Append(MessageText.Format(WhereToBuyToolMessage.FrameEnd));
            return text.ToString();
        }

        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.BranchesFound,
            new { search.Shop, Town = search.Town.Name ?? town, search.Branches.Count }));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TableHeader, new { Area = area }));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TableRule));
        foreach (var branch in search.Branches) text.AppendLine(Row(branch));
        text.AppendLine();
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.BranchesNote, new { search.Shop }));
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
