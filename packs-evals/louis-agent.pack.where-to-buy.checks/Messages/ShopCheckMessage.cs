using System.ComponentModel;

namespace LouisAgent.Packs.WhereToBuy.Messages;

/// <summary>
/// Why a shop rule passed or failed a conversation, in words a person reads in the portal: the pack's own checks'
/// reasons (F49.3), as its tool's texts are <see cref="WhereToBuyToolMessage"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopCheckMessage
{
    [Description("the list has a price column for a shop, though no shop's prices are read: {Shops}")]
    UntooledShopColumn,

    [Description("the list has no price column for a shop")]
    UntooledShopsNone,

    [Description("the scenario names no shop to price at")]
    ShopNotSet,

    [Description("turn {Turn} gave no answer to read the prices from")]
    ShopNothingToRead,

    [Description("the answer does not say the shop has no branch in the town")]
    ShopNoBranchNotSaid,

    [Description("a shop's page was read although no shop's prices are looked up")]
    ShopPageRead,

    [Description("a price is given as fact (linked to a shop, or without ±) although the user chose no shop")]
    ShopPriceForAny,

    [Description("the answer doesn't say the prices are estimates")]
    ShopEstimatesNotSaid,

    [Description("no shop's page was read, no price is linked, and the answer says the prices are estimates")]
    ShopAnyRight,


    [Description("the user named no shop, and the answer names none near them under Where to buy")]
    ShopAnyNamesNone,

    [Description("the user named no shop, and the shops near them were never looked up with {Tool}")]
    ShopAnyNotLookedUp,

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

    [Description(
        "Scenario '{Id}' expects a shop to price at, so it needs 'town' in expected: the town the cook names as the area of every shop call.")]
    NoTown,

    [Description("Scenario '{Id}': 'town' in expected must be a town's name.")]
    BadTown,

    [Description("no {Shop} tool was called")]
    ShopNotCalled,

    [Description("no page of {Shop}'s site was read")]
    ShopPageNotRead,

    [Description("priced at {Shop}: its page was read and the prices link to its site")]
    ShopPageRight,

    [Description("a {Shop} tool call did not name {Town} as its area")]
    ShopWrongArea,

    [Description("no price links to {Shop}'s site")]
    ShopPricesNotLinked,

    [Description("priced at {Shop}: its tool ran for the town and the prices link to its site")]
    ShopRight,
}
