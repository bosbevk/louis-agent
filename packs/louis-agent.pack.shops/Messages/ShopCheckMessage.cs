using System.ComponentModel;

namespace LouisAgent.Packs.Shops.Messages;

/// <summary>
/// Why a shop rule passed or failed a conversation, in words a person reads in the portal: the pack's own checks'
/// reasons (F49.3), as its tools' texts are <see cref="ShopToolMessage"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopCheckMessage
{
    [Description("the list has a column for a shop the cook can't read: {Shops}")]
    UntooledShopColumn,

    [Description("every shop the list has a column for is one the cook can read")]
    UntooledShopsNone,

    [Description("the scenario names no shop to price at")]
    ShopNotSet,

    [Description("turn {Turn} gave no answer to read the prices from")]
    ShopNothingToRead,

    [Description("the answer does not say the shop has no branch in the town")]
    ShopNoBranchNotSaid,

    [Description("a shop tool was called or a shop's page was read although the user chose no shop")]
    ShopCalledForAny,

    [Description("a price is given as fact (linked to a shop, or without ±) although the user chose no shop")]
    ShopPriceForAny,

    [Description("the answer doesn't say the prices are estimates")]
    ShopEstimatesNotSaid,

    [Description("nothing was searched, no price is linked, and the answer says the prices are estimates")]
    ShopAnyRight,

    [Description("the answer has no Where to buy section")]
    ShopNoWhereToBuySection,

    [Description("the Where to buy section names no {Shop} branch with a place (the shop and a place)")]
    ShopNoBranchWithPlace,

    [Description("the Where to buy section gives only a nearest {Shop} branch, with no place")]
    ShopOnlyNearestBranch,

    [Description("no shop page was read, no price is linked, the answer says the prices are estimates and names a "
                 + "{Shop} branch")]
    ShopEstimatesRight,

    [Description("Scenario '{Id}': 'shop' in expected must be one of: {Supported}.")]
    BadShop,
}
