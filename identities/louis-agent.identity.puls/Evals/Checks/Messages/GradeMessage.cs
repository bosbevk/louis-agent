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
}
