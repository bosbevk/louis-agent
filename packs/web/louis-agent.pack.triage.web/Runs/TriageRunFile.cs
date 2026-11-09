using System.Text.Json;

namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>
/// Reads a triage run as the pack's recorder wrote it (F106.3.4): one step per line, in the order of their numbers,
/// a blank line skipped. The demo plays a recorded run from it now, and a live run's steps arrive in the same shape.
/// </summary>
public static class TriageRunFile
{
    /// <summary>The run's steps, by number.</summary>
    public static IReadOnlyList<TriageStepView> Read(string jsonLines) =>
        jsonLines.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => JsonSerializer.Deserialize<TriageStepView>(line)!)
            .OrderBy(step => step.Number)
            .ToList();
}
