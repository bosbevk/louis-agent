using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>The toolsets the stub eval's modes carry (ADR 0043: an eval set keeps its own enum). A member's lower-case name is its wire name.</summary>
public enum StubToolset
{
    Specialists,
    Web,
}

public static class StubToolsets
{
    public static string Name(this StubToolset toolset) => SettingChoice.Wire(toolset);

    public static string[] Names(params StubToolset[] toolsets) => [.. toolsets.Select(Name)];
}
