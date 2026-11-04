using System.ComponentModel;

namespace LouisAgent.Packs.ShopPrices.Messages;

/// <summary>
/// What the shop-prices feed says about itself (ADR 0009, ADR 0070): its line in the list of feeds, and the notice above
/// its tables for whoever reads them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopPriceFeedMessage
{
    [Description(
        "The price tables the shop-prices tool estimates from: Stats SA's average prices per province, newest month, " +
        "and louis-agent's own rows for what Stats SA doesn't price.")]
    Description,

    [Description(
        "Average prices over many shops, not a shop's price. Each table names its source and month: Stats SA's (CPI " +
        "average prices, P0141) are free to reuse with credit to Statistics South Africa; louis-agent's own rows are " +
        "estimates of our own. Free to use.")]
    Notice,
}
