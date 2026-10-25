namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>Which seam an answer belongs to (F18 step 4b): written to the files as its snake_case name, never a number (ADR 0024).</summary>
public enum ReplayKind
{
    Visit,
    Search,
    Fetch,
    Clock,
}
