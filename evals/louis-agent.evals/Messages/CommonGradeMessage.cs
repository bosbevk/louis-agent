using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The reasons the harness's grades give (the common graders', and the runner's for a grader that threw), in words a person reads (ADR 0009: a text is a message enum member).</summary>
public enum CommonGradeMessage
{
    [Description("no turn raised an error")]
    NoErrorPass,

    [Description("turn {Turn} raised an error")]
    ErrorInTurn,

    [Description("turn {Turn} raised an error: {Message}")]
    ErrorInTurnWithMessage,

    [Description("every turn was answered within {Limit} s")]
    WithinTimePass,

    [Description("turn {Turn} took {Seconds} s, over {Limit} s")]
    SlowTurn,

    [Description("cost {Cost} is within the limit {Limit}")]
    CostPass,

    [Description("cost {Cost} is over the limit {Limit}")]
    CostOver,

    [Description("turn {Turn} reported no usage")]
    NoUsage,

    [Description("turn {Turn} had a request with no price")]
    Unpriced,

    [Description("turn {Turn} was priced in {Currency}, not USD")]
    OtherCurrency,

    [Description("routed to {Role}")] RoutedPass,

    [Description("routed to {Actual}, not {Role}")]
    RoutedElsewhere,

    [Description("answered itself, not {Role}")]
    AnsweredItself,

    [Description("there was no turn to route")]
    NoTurn,

    [Description("turn {Turn} failed for the infrastructure ({Failure}), so it can't be judged")]
    InfrastructureFailure,

    [Description("the turn failed")] TurnFailed,

    [Description("answered itself, as expected")]
    AnsweredAsExpected,

    [Description("handed to {Actual}, not answered itself")]
    HandedAway,

    [Description("the grader threw, so it can't tell: {Reason}")]
    GraderThrew,

    [Description("waits for a person to answer: {Question}")]
    HumanWaits,

    [Description("no judge configured")] NoJudge,

    [Description("the judge gave no yes-or-no verdict")]
    JudgeNoVerdict,

    [Description("the judge said yes")] JudgeSaidYes,

    [Description("the judge said no")] JudgeSaidNo,

    [Description("scenario '{Scenario}' is no longer in the dataset, so this conversation can't be graded")]
    ScenarioGone,
}
