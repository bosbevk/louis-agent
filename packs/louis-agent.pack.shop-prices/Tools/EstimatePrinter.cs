using System.Globalization;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Messages;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Tools;

/// <summary>
/// Writes a list's estimate for the model: a header that says where the prices are from, one table row per line
/// with what it was priced from, the total and the price per person, what couldn't be priced, and the credit line
/// Stats SA's terms ask for.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public static class EstimatePrinter
{
    /// <summary>The whole result, the line about an unknown province first when there is one.</summary>
    public static string Print(BasketEstimate estimate, PriceBook table, string? provinceUnknown)
    {
        string month = Month(table);
        string region = MessageText.Template(estimate.Region);
        var text = new StringBuilder();
        if (provinceUnknown is not null) text.AppendLine(provinceUnknown);
        text.AppendLine(MessageText.Format(ShopPriceToolMessage.Header, new
        {
            Region = region,
            Month = month
        }));
        text.AppendLine();
        text.AppendLine(MessageText.Format(ShopPriceToolMessage.TableHeader));
        text.AppendLine(MessageText.Format(ShopPriceToolMessage.TableRule));
        foreach (var line in estimate.Lines) text.AppendLine(Row(line, estimate.Region, table));

        text.AppendLine(MessageText.Format(ShopPriceToolMessage.TotalRow, new
        {
            Priced = estimate.Lines.Count - estimate.Unpriced.Count,
            estimate.Lines.Count,
            Total = Rand(estimate.Total),
            LeftOut = LeftOut(estimate.Unpriced.Count),
        }));
        if (estimate.PerPerson is { } perPerson)
            text.AppendLine(MessageText.Format(ShopPriceToolMessage.PerPersonRow, new
            {
                estimate.Serves,
                PerPerson = Rand(perPerson),
                LeftOut = LeftOut(estimate.Unpriced.Count),
            }));

        text.AppendLine();
        if (estimate.Unpriced.Count > 0)
            text.AppendLine(MessageText.Format(ShopPriceToolMessage.Unpriced,
                new { Items = string.Join(", ", estimate.Unpriced.Select(line => line.Item.Text)) }));
        text.Append(MessageText.Format(ShopPriceToolMessage.Credit, new
        {
            Region = region,
            Month = month
        }));
        return text.ToString();
    }

    /// <summary>One line's row: what it was priced from, or why it has no price.</summary>
    /// <remarks>A switch expression on how the estimate was made, one arm per kind (ADR 0051).</remarks>
    private static string Row(PriceEstimate line, Region region, PriceBook table) => line.Kind switch
    {
        EstimateKind.SamePack or EstimateKind.Scaled => Priced(ShopPriceToolMessage.LinePriced, line, region, table),
        EstimateKind.TablePack => Priced(ShopPriceToolMessage.LineTablePack, line, region, table),
        EstimateKind.OtherUnit => MessageText.Format(ShopPriceToolMessage.LineOtherUnit, new
        {
            Item = line.Item.Text,
            line.Match!.Product.Product,
            Sizes = string.Join(", ", line.Match.Product.Packs.Select(pack => pack.Size))
        }),
        _ => MessageText.Format(ShopPriceToolMessage.LineNotFound, new { Item = line.Item.Text }),
    };

    /// <summary>
    /// A priced line's row, saying when the national price stood in, when it is from an older month, and when the
    /// row is our own.
    /// </summary>
    private static string Priced(ShopPriceToolMessage row, PriceEstimate line, Region region, PriceBook table) =>
        MessageText.Format(row, new
        {
            Item = line.Item.Text,
            line.Match!.Product.Product,
            line.Pack!.Size,
            Price = line.Price!.Price.ToString("0.00", CultureInfo.InvariantCulture),
            Where = line.Price.Region != region
                ? MessageText.Format(ShopPriceToolMessage.NationalStandsIn,
                    new { Region = MessageText.Template(region) })
                : "",
            Ours = line.Match.File.Source != table.Files[0].Source
                ? MessageText.Format(ShopPriceToolMessage.OurOwnRow)
                : "",
            When = table.Files.Contains(line.Match.File)
                ? ""
                : MessageText.Format(ShopPriceToolMessage.OlderMonth, new { Month = MonthOf(line.Match.File.AsOf) }),
            Rand = Rand(line.Rand!.Value),
        });

    /// <summary>
    /// What goes after a total's amount so it can't look complete when it isn't: nothing, or how many items it leaves
    /// out.
    /// </summary>
    /// <remarks>A switch expression on the count, one arm per case (ADR 0051).</remarks>
    private static string LeftOut(int unpriced) => unpriced switch
    {
        0 => "",
        1 => MessageText.Format(ShopPriceToolMessage.LeftOutOne),
        _ => MessageText.Format(ShopPriceToolMessage.LeftOutMany, new { Unpriced = unpriced }),
    };

    /// <summary>Whole rand, as the list writes them.</summary>
    private static string Rand(decimal rand) => rand.ToString("0", CultureInfo.InvariantCulture);

    /// <summary>The month Stats SA's prices are for, as a person says it: "August 2026".</summary>
    private static string Month(PriceBook table) => MonthOf(table.Files[0].AsOf);

    /// <summary>A month as a person says it.</summary>
    private static string MonthOf(DateOnly month) => month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}
