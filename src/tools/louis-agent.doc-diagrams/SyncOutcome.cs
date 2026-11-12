namespace LouisAgent.DocDiagrams;

/// <summary>What happened to one diagram's SVG: written, already right, or (in a check run) found stale.</summary>
public enum SyncOutcome
{
    Written,
    Unchanged,
    Stale
}
