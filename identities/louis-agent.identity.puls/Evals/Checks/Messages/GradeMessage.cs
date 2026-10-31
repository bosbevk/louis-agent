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
}
