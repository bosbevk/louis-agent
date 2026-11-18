using System.Globalization;

namespace LouisAgent.Web.Framework.Content.Cost;

/// <summary>
/// One request worked through, as the engine prices it (F161.1, step 12): its model, its tokens in and out, the price
/// table's prices a million tokens for that model and the table's date, and the rand a dollar buys with the rates'
/// date. It works out the dollars and the rand itself, so the page shows sums, not numbers typed in.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): the sum is the engine's own, each kind of token times its price a million, plainly added,
/// as <c>CostCalculator</c> does, and the rand the dollars times the rate, as the display converts them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record CostExample(
    string Model,
    int InputTokens,
    int OutputTokens,
    decimal InputPerMillion,
    decimal OutputPerMillion,
    string PricesAsOf,
    decimal RandPerDollar,
    string RatesAsOf)
{
    /// <summary>A price is for a million tokens.</summary>
    private const decimal Million = 1_000_000m;

    /// <summary>What the request cost, in US dollars, as the ledger keeps it.</summary>
    public decimal Dollars => (InputTokens * InputPerMillion + OutputTokens * OutputPerMillion) / Million;

    /// <summary>What it cost in rand, at the rates' price of a dollar.</summary>
    public decimal Rand => Dollars * RandPerDollar;

    /// <summary>The dollars as the page writes them.</summary>
    public string DollarsText => "$" + Dollars.ToString("0.#####", CultureInfo.InvariantCulture);

    /// <summary>The rand as the page writes them, to four places.</summary>
    public string RandText => "R" + Rand.ToString("0.0000", CultureInfo.InvariantCulture);
}
