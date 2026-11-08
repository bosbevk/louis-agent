using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Runs.Steps;

/// <summary>How a run ended.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum TriageOutcome
{
    /// <summary>Nothing to do: the manager called the error expected or not worth it.</summary>
    [WireName("ignored")] Ignored,

    /// <summary>A fix is on a branch, checked and reviewed; never a pull request.</summary>
    [WireName("fixed-on-branch")] FixedOnBranch,

    /// <summary>A person has the case.</summary>
    [WireName("raised-to-person")] RaisedToPerson,

    /// <summary>Another department has the case, with its history.</summary>
    [WireName("handed-off")] HandedOff,

    /// <summary>The run itself failed, or a cap stopped it; the error stays unhandled for the next run.</summary>
    [WireName("failed")] Failed,
}
