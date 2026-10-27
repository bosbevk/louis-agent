using System.ComponentModel;

namespace LouisAgent.Evals.Puls.Messages;

/// <summary>What the portal calls Puls's evals (ADR 0009: a text is a message enum member, as the stub's is).</summary>
public enum PulsEvalMessage
{
    [Description(
        "Puls routes a meal to its cook, within the budget and at the shop that serves the town, and the conversation runs without a failed turn.")]
    CookProves,

    [Description("Puls sends each kind of question to the right place: a meal to its cook, the rest answered itself.")]
    RoutingProves,
}
