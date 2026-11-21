using LouisAgent.Choices;

namespace LouisAgent.Identities.Triage;

/// <summary>
/// The roles the manager asks (ADR 0043: each identity keeps its own enum of the names it uses), each
/// <c>specialists/triage-*.md</c>; a member's wire name is the role's file name, and the hosts carry it as text.
/// </summary>
public enum TriageRole
{
    /// <summary>Sends each error to its department.</summary>
    [WireName("triage-sorter")] Sorter,

    /// <summary>Digs into one error and reports where, why, whose area and how sure.</summary>
    [WireName("triage-investigator")] Investigator,

    /// <summary>Says whether a fix on its branch is good enough to keep.</summary>
    [WireName("triage-reviewer")] Reviewer,
}
