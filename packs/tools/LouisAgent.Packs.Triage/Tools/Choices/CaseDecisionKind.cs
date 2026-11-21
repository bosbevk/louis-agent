using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Tools.Choices;

/// <summary>What the manager decides a case gets.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum CaseDecisionKind
{
    /// <summary>Expected, or not worth it: nothing happens.</summary>
    [WireName("ignore")] Ignore,

    /// <summary>The investigator looks into it.</summary>
    [WireName("investigate")] Investigate,

    /// <summary>A person gets the case.</summary>
    [WireName("person")] Person,

    /// <summary>The coder fixes it on a branch.</summary>
    [WireName("auto-fix")] AutoFix,

    /// <summary>Another department's manager gets the case, with its history.</summary>
    [WireName("hand-off")] HandOff,
}
