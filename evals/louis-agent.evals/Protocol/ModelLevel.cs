namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The level a model is used at (F18 step 2a): the mode's own agent, a specialist it calls, a minion a specialist calls, or
/// a model grader. A specialist's role is separate (<see cref="ModelUse.Role"/>): two specialists are both Specialist.
/// </summary>
public enum ModelLevel
{
    Agent,
    Specialist,
    Minion,
    Grader,
}
