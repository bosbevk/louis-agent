namespace LouisAgent.Core.Config.ToolSearch;

/// <summary>
/// TOOL_SEARCH (F97.3): whether a chat sends only the tools it keeps loaded and finds the rest with a search, on a
/// provider that has one.
/// </summary>
public enum ToolSearchMode
{
    /// <summary>The default: on when a chat has more than ten tools that could wait to be found.</summary>
    Auto,

    /// <summary>Always on where the provider can search, for a test.</summary>
    On,

    /// <summary>Every tool is sent as before; the "without" of a measure.</summary>
    Off,
}
