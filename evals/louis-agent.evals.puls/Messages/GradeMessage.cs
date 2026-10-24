using System.ComponentModel;

namespace LouisAgent.Evals.Puls.Messages;

/// <summary>The graders' reasons, in words a person reads (ADR 0009: a text is a message enum member with its template).</summary>
public enum GradeMessage
{
    [Description("routed to the cook")]
    RoutedToCook,

    [Description("routed where its kind goes")]
    RoutedWhereKindGoes,

    [Description("no turn failed")]
    NoTurnFailed,

    [Description("{Count} turn(s) failed")]
    TurnsFailed,

    [Description("the turn failed")]
    TurnFailed,

    [Description("answered by Puls itself")]
    AnsweredByPuls,

    [Description("handled by {Role}")]
    HandledBy,
}
