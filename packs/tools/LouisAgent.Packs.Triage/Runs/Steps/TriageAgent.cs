using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Runs.Steps;

/// <summary>Who took a step of a run: one of the agents, or the workflow's own code between them.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum TriageAgent
{
    /// <summary>The workflow itself: the fixed steps, the checks and the caps.</summary>
    [WireName("code")] Code,

    /// <summary>Sends each error to its department.</summary>
    [WireName("sorter")] Sorter,

    /// <summary>The hub: decides what is worth investigating, a person or a fix, and checks everyone's work.</summary>
    [WireName("manager")] Manager,

    /// <summary>Digs into one error and reports where and why.</summary>
    [WireName("investigator")] Investigator,

    /// <summary>Fixes on a branch: the coding identity.</summary>
    [WireName("coder")] Coder,

    /// <summary>Says whether the fix is good enough.</summary>
    [WireName("reviewer")] Reviewer,
}
