namespace LouisAgent.Orchestrator;

/// <summary>
/// How the orchestrator's triage of one error ended: the outcomes its prompt offers on the closing DECISION line, plus
/// unknown when it wrote none we could read. Each travels as its lower-case name ("fixed") in decisions.jsonl and the
/// comms log.
/// </summary>
/// <remarks>
/// An enum, not a string (ADR 0043): the outcome is read once, where the model's line comes in, and the comms log
/// counts and filters on the member, so "fixed" can't be spelled two ways.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
internal enum TriageOutcome
{
    /// <summary>The runbook calls the error expected: nothing to do.</summary>
    Ignored,

    /// <summary>Handed to a human team.</summary>
    Escalated,

    /// <summary>louis-agent fixed it, and the orchestrator confirmed the fix.</summary>
    Fixed,

    /// <summary>The triage itself failed (a model error or a timeout), so the error is tried again next poll.</summary>
    Failed,

    /// <summary>The triage ended without a decision line we could read.</summary>
    Unknown,
}
