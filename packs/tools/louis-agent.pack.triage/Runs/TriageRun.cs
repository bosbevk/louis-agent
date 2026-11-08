using LouisAgent.Packs.Triage.Runs.Steps;

namespace LouisAgent.Packs.Triage.Runs;

/// <summary>A run as read back: its steps in order, and how it ended if it has.</summary>
/// <param name="Outcome">The finishing step's outcome; null while the run goes on.</param>
public sealed record TriageRun(string Id, IReadOnlyList<TriageStep> Steps, TriageOutcome? Outcome)
{
    /// <summary>A run from its saved steps, whatever order they came in; the outcome is the finishing step's.</summary>
    public static TriageRun Of(string id, IEnumerable<TriageStep> steps)
    {
        var ordered = steps.OrderBy(step => step.Number).ToList();
        return new TriageRun(id, ordered, ordered.LastOrDefault(step => step.Outcome is not null)?.Outcome);
    }
}
