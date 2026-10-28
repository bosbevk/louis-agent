using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// Writes what a run does to stdout, as protocol version 2: one event of the contract per line, the time from the
/// clock on each.
/// </summary>
/// <remarks>
/// Each method builds the contract's record for its event and hands it to <see cref="EvalProtocolJson"/>, so the
/// runner and the API share one shape and one set of words, and no line is written by hand (CL22).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed class ProtocolWriter(TextWriter stdout, TimeProvider clock)
{
    /// <summary>The version the runner writes; the run says it in its first line.</summary>
    public const int Version = ProtocolEvent.CurrentVersion;

    /// <summary>
    /// The run's first line: the eval, its version, the commit, the scenarios it asked for, the run it re-scores, the
    /// dataset it graded against, its graders and the model each mode ran on.
    /// </summary>
    public void Begin(string eval, int evalVersion, string commit, IReadOnlyList<IGrader> graders,
        IReadOnlyList<ModelInfo> models,
        RunSplit split = RunSplit.Dev, string? rescoreOf = null, DatasetManifest? dataset = null) =>
        Emit(new ProtocolEvent(Now, Version, eval, evalVersion, commit, split, rescoreOf, DatasetOf(dataset),
            [.. graders.Select(GraderOf)], models));

    /// <summary>A mode's repeat starts: its toolsets, role and model, and how many conversations it will run.</summary>
    public void Mode(string mode, int repeat, IReadOnlyList<string> toolsets, string? role, string? llm,
        int? conversations = null) =>
        Emit(new ModeEvent(Now, mode, repeat, toolsets, role, llm, conversations));

    /// <summary>The question put to the session on a turn.</summary>
    public void Question(string mode, int repeat, string task, int turn, string session, string text) =>
        Emit(new QuestionEvent(Now, mode, repeat, task, turn, session, text));

    /// <summary>One event of a turn's stream, as the session sent it: its type and its JSON.</summary>
    public void Sse(string mode, int repeat, string task, int turn, string type, JsonElement data) =>
        Emit(new SseEvent(Now, mode, repeat, task, turn, type, data));

    /// <summary>
    /// A turn: what was asked and answered, what the eval says about it, and the failure on our side that ended it
    /// early, if one did.
    /// </summary>
    public void Turn(string mode, int repeat, string task, int turn, string session, string question, string? answer,
        string? role, double seconds, TurnFacts facts, TurnFailure? failure = null) =>
        Emit(new TurnEvent(Now, mode, repeat, task, turn, session, question, answer, role, facts.Continued,
            facts.AgentCalls, facts.SpecialistCalls, facts.Hosts, seconds, facts.Error, failure));

    /// <summary>
    /// A conversation's result: its grades and outcome. The eval's own <paramref name="scores"/> are no part of
    /// version 2 and are written nowhere; step 7c.2d takes them out, and measures the conversation instead, so every
    /// metric is unknown until then.
    /// </summary>
    public void Conversation(string mode, int repeat, string task, string? role, JsonElement scores, double seconds,
        IReadOnlyList<Grade> grades, TrialOutcome outcome, int errors) =>
        Emit(new ConversationEvent(Now, mode, repeat, task, role, errors, seconds, [.. grades.Select(GradeOf)],
            outcome, new Metrics(null, null, null, null, null, null, null, null)));

    /// <summary>
    /// A mode's repeat is done. With <paramref name="k"/> the line also carries the mode's pass@k and pass^k over the
    /// repeats so far (null when no trial counted, never 0), and how many trials were left out for the infrastructure
    /// or run twice.
    /// </summary>
    public void ModeDone(string mode, int repeat, PassShare? rates = null, int? k = null, int? infrastructure = null,
        int? retried = null) =>
        Emit(new ModeDoneEvent(Now, mode, repeat,
            k is null
                ? null
                : new RatesInfo(rates?.PassAtK, rates?.PassHatK, k.Value, infrastructure ?? 0,
                    retried ?? 0)));

    /// <summary>One slice of a mode's roll-up, written after the mode's last repeat.</summary>
    public void Summary(SummaryLine line) =>
        Emit(new SummaryEvent(Now, line.Mode, line.Slice, line.N, line.Passed, line.Failed, line.Unknown,
            line.Infrastructure, line.Tasks, line.TooFew, line.PassRate, line.PassAtK, line.PassHatK, line.Graders,
            line.Kinds));

    /// <summary>A line of a stored run, written as it was: a re-score keeps the lines it grades again.</summary>
    public void Copy(string line) => Write(line);

    /// <summary>The time on the line being written, in UTC.</summary>
    private DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>The dataset as the first line names it: its name, its version and the hash of its scenarios.</summary>
    private static DatasetInfo? DatasetOf(DatasetManifest? dataset) =>
        dataset is null ? null : new DatasetInfo(dataset.Name, dataset.Version, dataset.ContentHash);

    /// <summary>A grader as the first line names it; a human review carries the question a person is asked.</summary>
    private static GraderInfo GraderOf(IGrader grader) =>
        new(grader.Name, grader.Version, grader.Kind, (grader as HumanReviewGrader)?.Question);

    /// <summary>
    /// A grade as the conversation carries it, with the judge's model and prompt when a model judged.
    /// </summary>
    private static GradeInfo GradeOf(Grade grade) =>
        new(grade.Grader, grade.Version, grade.Score, grade.Outcome, grade.Reason, grade.Judge?.Model,
            grade.Judge?.PromptHash);

    /// <summary>Writes one event as its line.</summary>
    private void Emit(EvalEvent e) => Write(EvalProtocolJson.Line(e));

    /// <summary>Writes one line and flushes it, so the API reads it as it happens.</summary>
    private void Write(string line)
    {
        stdout.Write(line);
        stdout.Write('\n');
        stdout.Flush();
    }
}
