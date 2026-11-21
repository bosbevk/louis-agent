namespace LouisAgent.Core.Usage.Records;

using LouisAgent.Core.Pricing.Prices;


/// <summary>
/// A record's cost, stored with it so a later price change doesn't rewrite history (Usage spec §4.1).
/// </summary>
/// <param name="Currency">The price table's currency, e.g. "USD".</param>
/// <param name="Amount">Rounded to 6 decimals; round further only for display.</param>
/// <param name="PriceTable">The table's <c>as_of</c> date: which prices this cost used.</param>
public sealed record UsageCost(string Currency, decimal Amount, string? PriceTable);
