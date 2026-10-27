using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The level a model is used at, as the protocol names it (F18 step 2a; parsed once, with <c>SettingChoice</c>). A specialist's
/// role is separate: two specialists are both Specialist.
/// </summary>
public enum BenchmarkModelLevel
{
    Agent,
    Specialist,
    Minion,
    Grader,
}
