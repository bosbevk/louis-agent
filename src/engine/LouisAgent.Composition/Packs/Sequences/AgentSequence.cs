namespace LouisAgent.Composition.Packs.Sequences;

/// <summary>
/// A fixed order of agents an identity runs one after the other, each step an address from its own
/// <see cref="IAgentIdentity.Calls"/> (<c>triage.reader</c>, or <c>partner:shop.cook</c> on another server). Code
/// walks the steps, so the order never depends on what a model decides.
/// </summary>
/// <remarks>
/// A Value Object (Fowler): a name, the steps in order and the two choices, which an identity declares and the engine
/// only reads. It is the "workflow" of Anthropic's Building effective agents, agents on a predefined code path,
/// beside the agent that picks whom to ask with <c>AskAgent</c>.
/// </remarks>
/// <param name="Name">What the identity calls it, in lowercase letters, digits and dashes: <c>fix-an-error</c>.</param>
/// <param name="Steps">The addresses asked, in order.</param>
/// <param name="Input">What each step after the first is given.</param>
/// <param name="Failure">What happens when a step fails.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record AgentSequence(
    string Name,
    IReadOnlyList<string> Steps,
    StepInput Input = StepInput.FirstMessageAndPreviousAnswer,
    StepFailure Failure = StepFailure.Stop);
