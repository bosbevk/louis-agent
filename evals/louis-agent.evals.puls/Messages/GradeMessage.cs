using System.ComponentModel;

namespace LouisAgent.Evals.Puls.Messages;

/// <summary>The graders' reasons, in words a person reads (ADR 0009: a text is a message enum member with its template).</summary>
public enum GradeMessage
{
    [Description("no turn failed")] NoTurnFailed,

    [Description("{Count} turn(s) failed")]
    TurnsFailed,

    [Description("the scenario asks for no number of people")]
    ServesNotAsked,

    [Description("every answer names the number of people asked for")]
    ServesRight,

    [Description("turn {Turn} gave no answer to read the number of people from")]
    ServesNothingToRead,

    [Description("turn {Turn} doesn't say it serves {Asked}")]
    ServesForgotten,

    [Description("turn {Turn} says it serves {Found}, not the {Asked} asked for")]
    ServesWrong,

    [Description("the scenario asks for no number of people, so there is no meal to judge")]
    MealNotAsked,

    [Description("the judge's reply is not a verdict: {Reply}")]
    JudgeReplyUnreadable,

    [Description("the scenario sets no budget")]
    BudgetNotSet,

    [Description("the answer gives no amount to read a basket total from")]
    BudgetNoAmount,

    [Description("the total R{Total} (from the {Source}) is within the budget R{Budget}")]
    BudgetWithin,

    [Description("the total R{Total} (from the {Source}) is over the budget R{Budget}")]
    BudgetOver,

    [Description("total line")] SourceTotalLine,

    [Description("sum of the priced rows")]
    SourceRowSum,

    [Description("shopping list's total row")]
    SourceListTotalRow,

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

    [Description(
        "no shop page was read, no price is linked, the answer says the prices are estimates and names a {Shop} branch")]
    ShopEstimatesRight,

    [Description("the scenario asks for no image of the recipe")]
    RecipeImageNotSet,

    [Description("no page the cook read had an image to show")]
    RecipeImageNoneOnPages,

    [Description("the answer shows no image of the recipe, though the recipe page had one")]
    RecipeImageMissing,

    [Description("the answer's image is not one the recipe page gave")]
    RecipeImageNotFromPage,

    [Description("the answer shows an image the recipe page gave")]
    RecipeImageRight,
}
