using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Runs.Steps;

/// <summary>What kind of step a run took, so a page can draw it and a reader can filter on it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum TriageStepKind
{
    /// <summary>An error was read from the feed and the run opened.</summary>
    [WireName("collected")] Collected,

    /// <summary>The sorter named the department.</summary>
    [WireName("sorted")] Sorted,

    /// <summary>The manager decided what the case gets: nothing, an investigation, a person, a fix, a hand-off.</summary>
    [WireName("decided")] Decided,

    /// <summary>The investigator reported where and why.</summary>
    [WireName("investigated")] Investigated,

    /// <summary>The coder reported a fix on a branch.</summary>
    [WireName("fixed")] Fixed,

    /// <summary>The code checked the fix: the commit, the replay, the tests.</summary>
    [WireName("checked")] Checked,

    /// <summary>The reviewer said whether it is happy.</summary>
    [WireName("reviewed")] Reviewed,

    /// <summary>The case was raised to a person.</summary>
    [WireName("raised")] Raised,

    /// <summary>The case was handed to another department with its history.</summary>
    [WireName("handed-off")] HandedOff,

    /// <summary>The run ended, with its outcome.</summary>
    [WireName("finished")] Finished,
}
