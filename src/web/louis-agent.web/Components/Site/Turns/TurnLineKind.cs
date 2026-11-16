namespace LouisAgent.Web.Components.Site.Turns;

/// <summary>What a line of a playing turn is, which decides how the window shows it.</summary>
public enum TurnLineKind
{
    /// <summary>What was asked, or what a run reported back.</summary>
    Prompt,

    /// <summary>The agent thinking, quiet and slanted.</summary>
    Think,

    /// <summary>A tool the agent called, with its argument.</summary>
    Tool,

    /// <summary>What a guard did with a tool's result, set in.</summary>
    Guard,

    /// <summary>The agent's answer, with the caret after it.</summary>
    Answer
}
