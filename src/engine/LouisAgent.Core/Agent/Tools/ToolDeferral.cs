using LouisAgent.Core.Config.ToolSearch;

namespace LouisAgent.Core.Agent.Tools;

/// <summary>
/// Which of an engine's tools wait to be found (F97.3): decided once its tools are registered, every tool but the ones
/// its toolsets keep loaded and the engine's own, and only when the provider can search and TOOL_SEARCH says so.
/// </summary>
/// <param name="mode">
/// TOOL_SEARCH: on always, off never, auto past <see cref="AutoAbove"/> tools that could wait.
/// </param>
/// <param name="kept">The tools the engine's toolsets keep loaded (<c>metadata.loaded</c>).</param>
internal sealed class ToolDeferral(ToolSearchMode mode, IReadOnlySet<string> kept)
{
    /// <summary>
    /// How many tools could wait before auto turns search on, Anthropic's own advice: from ten tools up.
    /// </summary>
    public const int AutoAbove = 10;

    /// <summary>Not decided yet: nothing waits.</summary>
    public bool On { get; private set; }

    /// <summary>The names of the tools that wait to be found; none while search is off.</summary>
    public IReadOnlySet<string> Deferred { get; private set; } = new HashSet<string>();

    /// <summary>The engine's own tools, which every chat that has them keeps loaded.</summary>
    private static readonly HashSet<string> EngineOwn =
        ["FindTool", "GetToday", "UseSkill", "CreateSkill", "ReloadSkills", "ReadSkill"];

    /// <summary>Decides, from the engine's registered tools, whether search is on and which tools wait.</summary>
    public void Decide(IEnumerable<string> registered, bool searchAvailable)
    {
        var couldWait = registered.Where(name => !kept.Contains(name) && !EngineOwn.Contains(name)).ToHashSet();
        On = searchAvailable && IsOn(couldWait.Count);
        Deferred = On ? couldWait : new HashSet<string>();
    }

    /// <summary>What TOOL_SEARCH says for this many tools that could wait.</summary>
    private bool IsOn(int couldWait) => mode switch
    {
        ToolSearchMode.On => true,
        ToolSearchMode.Off => false,
        _ => couldWait > AutoAbove
    };
}
