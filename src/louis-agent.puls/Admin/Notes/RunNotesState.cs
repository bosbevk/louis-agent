namespace LouisAgent.Puls.Admin.Notes;

/// <summary>
/// Lets the form tell the run page a note was written, so the page fetches the run again and every panel shows what's stored. It keeps no notes itself, because a copy here could go stale.
/// </summary>
/// <remarks>
/// Observer. The form raises one event and whoever is showing notes reacts, so the form never needs to know who that is. Without it the page would have to be handed the new note, and then it would hold a copy of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public sealed class RunNotesState
{
    /// <summary>Raised once the API has taken a note.</summary>
    public event Action? Changed;

    /// <summary>The form calls this after a note is saved.</summary>
    public void Written() => Changed?.Invoke();
}
