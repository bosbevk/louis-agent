using System.ComponentModel;

namespace LouisAgent.Identities.Puls.Evals.Checks.Messages;

/// <summary>Why a grader passed or failed a conversation, in words a person reads in the portal.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum GradeMessage
{
    [Description("the scenario asks for no number of people, so there is no meal to judge")]
    MealNotAsked,

    [Description("the judge's reply is not a verdict: {Reply}")]
    JudgeReplyUnreadable,

    [Description("the scenario names no shop to price at")]
    ShopNotSet,

    [Description("turn {Turn} gave no answer to read the prices from")]
    ShopNothingToRead,

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
}
