using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The texts the eval shows a person about a task (ADR 0009: a message enum member with its template).</summary>
public enum EvalMessage
{
    [Description("Scenario '{Scenario}' has no messages, so it is skipped.")]
    NoMessages,
}
