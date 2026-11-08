using System.ComponentModel;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages;
using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;
using LouisAgent.Packs.Triage.Tools.Results;

namespace LouisAgent.Packs.Triage.Tools;

/// <summary>
/// The <c>triage</c> toolset: how a role ends its work with a typed result instead of prose. The investigator
/// reports where and why, the reviewer whether it is happy, the manager what the case gets; each call is checked
/// in code and saved as a step of the run, which the workflow, the demo page and the guide read back.
/// </summary>
/// <remarks>
/// A tool, not a prompt rule (ADR 0080): asking a model to end with a typed tool call is what makes its answer a
/// record the code can check. Every value is a plain string parsed once into its enum (ADR 0024); a bad one is
/// refused as text with the choices, never a thrown exception (the pack contract). The run id arrives as a
/// parameter, since a tool reads nothing from the session.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed class TriageTools(ITriageRecorder recorder, TimeProvider time)
{
    /// <summary>The investigator's ending: where the cause is, what it is, whose area, how sure.</summary>
    [Description(
        "Ends an investigation with what was found. Call it once, as the last thing you do: where the cause is (a " +
        "file and method, or a component), what the cause is in one sentence, whose area it is and how sure you are.")]
    public async Task<string> ReportInvestigation(
        [Description("The run id you were given.")]
        string runId,
        [Description("Where the cause is, such as 'OrderApi.GetOrderTotal in Api/OrderTotal.cs'.")]
        string where,
        [Description("The cause, in one sentence.")]
        string cause,
        [Description("Whose area it is: code, data, hosting, third-party or unknown.")]
        string area,
        [Description("How sure you are: low, medium or high.")]
        string confidence,
        CancellationToken cancellationToken = default)
    {
        if (!SettingChoice.TryParse(area, out DepartmentArea? theArea) || theArea is not { } a)
            return NotAChoice(area, "department area", SettingChoice.Names<DepartmentArea>());
        if (!SettingChoice.TryParse(confidence, out Confidence? theConfidence) || theConfidence is not { } c)
            return NotAChoice(confidence, "confidence", SettingChoice.Names<Confidence>());
        if (string.IsNullOrWhiteSpace(cause))
            return MessageText.Format(TriageToolMessage.NoReason, new { What = "cause" });

        var found = new Investigation(where.Trim(), cause.Trim(), a, c);
        return await RecordAsync(runId, TriageAgent.Investigator, TriageStepKind.Investigated,
            $"The investigator found the cause in {found.Where}: {found.Cause}",
            $"The area is {a.Wire()}, confidence {c.Wire()}.", found, cancellationToken);
    }

    /// <summary>The reviewer's ending: happy or not, with the reasons.</summary>
    [Description(
        "Ends a review. Call it once, as the last thing you do: whether the fix is good enough to keep, and your " +
        "reasons, one per line. A review that isn't happy goes back to the coder through the manager.")]
    public async Task<string> ReportReview(
        [Description("The run id you were given.")]
        string runId,
        [Description("True when the fix is good enough to keep.")]
        bool happy,
        [Description("Your reasons, one per line; at least one.")]
        string reasons,
        CancellationToken cancellationToken = default)
    {
        var lines = reasons.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
        if (lines.Count == 0) return MessageText.Format(TriageToolMessage.NoReason, new { What = "review" });

        var review = new Review(happy, lines);
        string summary = happy ? "The reviewer is happy with the fix." : "The reviewer is not happy with the fix.";
        return await RecordAsync(runId, TriageAgent.Reviewer, TriageStepKind.Reviewed, summary,
            string.Join(" ", lines), review, cancellationToken);
    }

    /// <summary>The manager's ending: what the case gets, and why.</summary>
    [Description(
        "Ends the manager's decision on a case. Call it once, as the last thing you do: ignore it, investigate it, " +
        "give it to a person, let the coder fix it, or hand it to another department; and why, in a sentence.")]
    public async Task<string> DecideCase(
        [Description("The run id you were given.")]
        string runId,
        [Description("The decision: ignore, investigate, person, auto-fix or hand-off.")]
        string kind,
        [Description("Why, in a sentence.")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (!SettingChoice.TryParse(kind, out CaseDecisionKind? theKind) || theKind is not { } k)
            return NotAChoice(kind, "decision", SettingChoice.Names<CaseDecisionKind>());
        if (string.IsNullOrWhiteSpace(reason))
            return MessageText.Format(TriageToolMessage.NoReason, new { What = "decision" });

        var decision = new CaseDecision(k, reason.Trim());
        return await RecordAsync(runId, TriageAgent.Manager, TriageStepKind.Decided,
            $"The manager decided: {k.Wire()}.", decision.Reason, decision, cancellationToken);
    }

    /// <summary>Saves the step with its typed record as JSON, numbered after the run's last, and says so.</summary>
    private async Task<string> RecordAsync<T>(string runId, TriageAgent agent, TriageStepKind kind, string summary,
        string why, T result, CancellationToken cancellationToken)
    {
        if (!PlainId.Is(runId)) return MessageText.Format(TriageToolMessage.BadRunId, new { RunId = runId });
        var run = await recorder.ReadAsync(runId, cancellationToken);
        if (run is null) return MessageText.Format(TriageToolMessage.NoSuchRun, new { RunId = runId });

        var step = new TriageStep(runId, run.Steps.Count + 1, agent, kind, summary, why, time.GetUtcNow(),
            Result: JsonSerializer.Serialize(result, FileTriageRecorder.Json));
        await recorder.SaveAsync(step, cancellationToken);
        return MessageText.Format(TriageToolMessage.Recorded, new
        {
            step.Number,
            RunId = runId,
            Summary = summary
        });
    }

    /// <summary>The refusal for a value that isn't one of the choices, naming them.</summary>
    private static string NotAChoice(string value, string what, IReadOnlyList<string> options) =>
        MessageText.Format(TriageToolMessage.NotAChoice, new
        {
            Value = value,
            What = what,
            Options = string.Join(", ", options)
        });
}
