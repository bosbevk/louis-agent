using System.ComponentModel;
using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages;
using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;

namespace LouisAgent.Packs.Triage.Tools;

/// <summary>
/// The rest of the <c>triage</c> toolset: a run read back as text, for anyone who holds the toolset (the guide in the
/// site's corner, a person at the terminal, the manager checking what happened), and a case raised to a person,
/// which writes the case where people read and records the step.
/// </summary>
/// <remarks>
/// Apart from <see cref="TriageTools"/> by responsibility (ADR 0034): those tools end a role's work typed, these
/// read a run and reach a person. A person's endpoint is a folder of markdown today (ADR 0030, the simplest thing
/// that works); a department's own endpoint is a later setting.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class RunTools(ITriageRecorder recorder, string peopleFolder, TimeProvider time)
{
    /// <summary>A run's steps, one line each, so the reader can say what happened and why.</summary>
    [Description(
        "Reads the run back: every step so far, one line each, with who did it, what and why, and how the run " +
        "ended if it has. Use it to explain what is happening in a run, or to see what came before you.")]
    public async Task<string> ReadTriageRun(
        [Description("The run id.")] string runId,
        CancellationToken cancellationToken = default)
    {
        if (!PlainId.Is(runId)) return MessageText.Format(TriageToolMessage.BadRunId, new { RunId = runId });
        var run = await recorder.ReadAsync(runId, cancellationToken);
        if (run is null) return MessageText.Format(TriageToolMessage.NoSuchRun, new { RunId = runId });

        string ended = run.Outcome is { } outcome
            ? MessageText.Format(TriageToolMessage.RunEnded, new { Outcome = outcome.Wire() })
            : "";
        return MessageText.Format(TriageToolMessage.RunRead, new
        {
            RunId = runId,
            Count = run.Steps.Count,
            Ended = ended,
            Steps = string.Join('\n', run.Steps.Select(Line))
        });
    }

    /// <summary>Gives a case to a person: the case as a file where people read, and a step that says so.</summary>
    [Description(
        "Raises a case to a person instead of fixing it: when the investigator isn't sure, when the fix would touch " +
        "money or data, or when no rule allows an automatic fix. Say what the case is and why a person is needed.")]
    public async Task<string> RaiseToPerson(
        [Description("The run id you were given.")] string runId,
        [Description("What the case is, in a sentence or two.")] string summary,
        [Description("Why a person is needed, in a sentence.")] string why,
        CancellationToken cancellationToken = default)
    {
        if (!PlainId.Is(runId)) return MessageText.Format(TriageToolMessage.BadRunId, new { RunId = runId });
        if (string.IsNullOrWhiteSpace(why)) return MessageText.Format(TriageToolMessage.NoReason, new { What = "case" });
        var run = await recorder.ReadAsync(runId, cancellationToken);
        if (run is null) return MessageText.Format(TriageToolMessage.NoSuchRun, new { RunId = runId });

        // The case where people read: the summary, the reason, and the run so far, so nobody has to find the run file.
        Directory.CreateDirectory(peopleFolder);
        string path = Path.Combine(peopleFolder, runId + ".md");
        var now = time.GetUtcNow();
        string lines = string.Join('\n', run.Steps.Select(step => "- " + Line(step)));
        await File.WriteAllTextAsync(path,
            $"# Case {runId}\n\nRaised {now:yyyy-MM-ddTHH:mm:ssZ}.\n\n{summary.Trim()}\n\n**Why a person:** {why.Trim()}\n\n## The run so far\n\n{lines}\n",
            cancellationToken);

        var step = new TriageStep(runId, run.Steps.Count + 1, TriageAgent.Manager, TriageStepKind.Raised,
            $"The manager raised the case to a person: {summary.Trim()}", why.Trim(), now);
        await recorder.SaveAsync(step, cancellationToken);
        return MessageText.Format(TriageToolMessage.Raised, new { RunId = runId, Path = path, step.Number });
    }

    /// <summary>One step as a line a person reads.</summary>
    private static string Line(TriageStep step) =>
        MessageText.Format(TriageToolMessage.StepLine, new
        {
            step.Number,
            Agent = step.Agent.Wire(),
            Kind = step.Kind.Wire(),
            At = step.At.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            step.Summary,
            step.Why
        }).TrimEnd();
}
