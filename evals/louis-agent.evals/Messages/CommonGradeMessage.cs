using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The reasons the common graders give, in words a person reads (ADR 0009: a text is a message enum member).</summary>
public enum CommonGradeMessage
{
    [Description("no turn raised an error")]
    NoErrorPass,

    [Description("turn {Turn} raised an error")]
    ErrorInTurn,

    [Description("every turn was answered within {Limit} s")]
    WithinTimePass,

    [Description("turn {Turn} took {Seconds} s, over {Limit} s")]
    SlowTurn,

    [Description("cost {Cost} is within the limit {Limit}")]
    CostPass,

    [Description("cost {Cost} is over the limit {Limit}")]
    CostOver,

    [Description("a request had no price, so the cost is unknown")]
    CostUnknown,

    [Description("routed to {Role}")]
    RoutedPass,

    [Description("routed to {Actual}, not {Role}")]
    RoutedElsewhere,

    [Description("answered itself, not {Role}")]
    AnsweredItself,

    [Description("there was no turn to route")]
    NoTurn,
}
