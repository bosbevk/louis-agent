using LouisAgent.Packs.Triage.Runs.Steps;

namespace LouisAgent.Packs.Triage.Runs;

/// <summary>
/// Where a run's steps are kept as they land, so a page can follow a run while it goes, the guide can read it, and
/// a restart finds where it was.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four): a file per run today, the saved conversations' store when F43 lands, and the workflow
/// and the tools never know which.
/// </remarks>
public interface ITriageRecorder
{
    /// <summary>Keeps one step; a step is never changed once saved.</summary>
    Task SaveAsync(TriageStep step, CancellationToken cancellationToken = default);

    /// <summary>A run with its steps in order, or nothing when no step of it was ever saved.</summary>
    Task<TriageRun?> ReadAsync(string runId, CancellationToken cancellationToken = default);
}
