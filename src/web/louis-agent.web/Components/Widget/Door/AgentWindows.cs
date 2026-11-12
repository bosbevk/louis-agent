using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// Which window on a page shows which agent (F127.4.2): a showroom's live window says the business it shows, and the
/// guide's widget hands it an ask to that business, so the visitor watches the talk in the business's own window and
/// carries on there.
/// </summary>
/// <remarks>
/// Observer, as <see cref="GuideDoor"/> is: one small shared object the site registers once; windows say what they
/// show and listen, the widget hands over. Of three (a script event between the two, a second channel beside
/// Blazor's; the showroom page passing parameters, which can't reach the widget in the layout's corner), the one the
/// framework already uses.
/// </remarks>
public sealed class AgentWindows
{
    private readonly HashSet<string> _shown = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised with an ask to an agent a window shows: the window takes the talk.</summary>
    public event Action<Block>? Asked;

    /// <summary>Raised as the guide's stream moves on, so a window showing an ask's talk draws again.</summary>
    public event Action? Moved;

    /// <summary>A window shows this agent until the returned handle is disposed.</summary>
    public IDisposable Show(string identity)
    {
        _shown.Add(identity);
        return new Shown(() => _shown.Remove(identity));
    }

    /// <summary>Whether a window shows this agent.</summary>
    public bool Shows(string identity) => _shown.Contains(identity);

    /// <summary>
    /// Hands an ask to the window that shows its agent, when one does; any other call stays the guide's.
    /// </summary>
    public bool Take(Block call)
    {
        if (call.ToolName != Delegation.AskToolName || Delegation.From(call) is not { } ask) return false;
        if (!Shows(ask.Agent)) return false;
        Asked?.Invoke(call);
        return true;
    }

    /// <summary>Tells the windows the guide's stream moved on.</summary>
    public void Move() => Moved?.Invoke();

    /// <summary>A window's hold on its agent, let go when disposed.</summary>
    private sealed class Shown(Action release) : IDisposable
    {
        /// <summary>Lets go of the agent.</summary>
        public void Dispose() => release();
    }
}
