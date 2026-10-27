using LouisAgent.Choices;

namespace LouisAgent.Core.Agent.Toolsets;

public static class CoreToolsets
{
    /// <summary>The name a session, a request or a role file knows the toolset by.</summary>
    public static string Name(this CoreToolset toolset) => SettingChoice.Wire(toolset);

    /// <summary>The names, for a list a session or a request carries.</summary>
    public static string[] Names(params CoreToolset[] toolsets) => [.. toolsets.Select(Name)];
}
