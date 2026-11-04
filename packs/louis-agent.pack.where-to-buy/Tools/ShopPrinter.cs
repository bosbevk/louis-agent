using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.WhereToBuy.Messages;
using LouisAgent.Packs.WhereToBuy.Shops;

namespace LouisAgent.Packs.WhereToBuy.Tools;

/// <summary>
/// Writes a lookup of the supermarkets near a town for the model, framed as data since the map's names are what its
/// mappers typed: the town's municipality and province, a row per shop nearest first, and what to say when none was
/// found. It writes the rows as <see cref="BranchPrinter"/> does.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public static class ShopPrinter
{
    /// <summary>The whole result.</summary>
    public static string Print(ShopsNearby nearby, string town)
    {
        string name = nearby.Town.Name ?? town;
        string area = nearby.Town.Address?.Area ?? town;
        string province = nearby.Town.Address?.State ?? "South Africa";
        var text = new StringBuilder();
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.UntrustedFrame));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TownFound,
            new
            {
                Town = name,
                Area = area,
                Province = province
            }));
        text.AppendLine();
        if (nearby.Shops.Count == 0)
        {
            text.AppendLine(MessageText.Format(WhereToBuyToolMessage.NoShop,
                new
                {
                    Km = nearby.SearchedKm,
                    Town = name
                }));
            text.Append(MessageText.Format(WhereToBuyToolMessage.FrameEnd));
            return text.ToString();
        }

        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.ShopsFound,
            new
            {
                Town = name,
                nearby.Shops.Count
            }));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.ShopsTableHeader, new { Area = area }));
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.TableRule));
        foreach (var shop in nearby.Shops) text.AppendLine(BranchPrinter.Row(shop));
        text.AppendLine();
        text.AppendLine(MessageText.Format(WhereToBuyToolMessage.ShopsNote, new { Area = area }));
        text.Append(MessageText.Format(WhereToBuyToolMessage.FrameEnd));
        return text.ToString();
    }
}
