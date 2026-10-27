using LouisAgent.Choices;
namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The level a model is used at (F18 step 2a): the mode's own agent, a specialist it calls, a minion a specialist calls, or
/// a model grader. A specialist's role is separate (<see cref="ModelUse.Role"/>): two specialists are both Specialist.
/// </summary>
public enum ModelLevel
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Agent")] Agent,
    [WireName("Specialist")] Specialist,
    [WireName("Minion")] Minion,
    [WireName("Grader")] Grader,
}
