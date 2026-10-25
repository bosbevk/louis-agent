namespace LouisAgent.Puls.Admin.Prices;

/// <summary>One chip of a conversation's row: its text, and its kind (<c>ok</c>, <c>bad</c>, <c>warn</c>, or none).</summary>
public sealed record PricedChip(string Text, string Kind);

/// <summary>
/// The chips for a conversation's shopping list (FX24): how many rows were priced, and how many of those were estimates. The cook gives estimates while the
/// stores' answer is awaited, so an estimated row counts as priced; the estimates are said apart, so a shop's own price is never mistaken for one. A scores
/// document from before the estimates has no count and shows the chip it always did.
/// </summary>
public static class PricedChips
{
    public static IReadOnlyList<PricedChip> Of(int priced, int? estimated, int items, decimal ratio)
    {
        int estimates = estimated ?? 0;
        var chips = new List<PricedChip> { new($"priced {priced + estimates} / {items}", ratio >= 0.8m ? "ok" : ratio < 0.5m ? "bad" : "") };
        if (estimates > 0) chips.Add(new PricedChip($"estimated {estimates}", ""));
        return chips;
    }
}
