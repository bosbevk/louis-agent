using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The line protocol the eval writes to stdout (F31's version 1): <c>@</c> and one JSON object per event, with the
/// event's name, the time <c>t</c> in seconds from the clock, and the event's fields in the order the Python harness
/// wrote them. ASCII only: every other character is escaped, so any console can show the line and JSON reads it back as
/// it was.
/// </summary>
public sealed class ProtocolWriter(TextWriter stdout, TimeProvider clock)
{
    /// <summary>The version the API reads; the eval says it in its first line.</summary>
    public const int Version = 1;

    private static readonly JsonWriterOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),
    };

    /// <summary>
    /// The old first line, the version alone. Kept only for the parity test against the Python's first line; a run
    /// writes <see cref="Begin"/>, which carries the eval.
    /// </summary>
    public void Started() => Emit("protocol", json => json.WriteNumber("version", Version));

    /// <summary>A mode's repeat starts: its toolsets, role and model, and how many conversations it will run.</summary>
    public void Mode(string mode, int repeat, IReadOnlyList<string> toolsets, string? role, string? llm,
        int? conversations = null) =>
        Emit("mode", json =>
        {
            json.WriteString("mode", mode);
            json.WriteNumber("repeat", repeat);
            json.WriteStartArray("toolsets");
            foreach (var toolset in toolsets) json.WriteStringValue(toolset);
            json.WriteEndArray();
            WriteText(json, "role", role);
            WriteText(json, "llm", llm);
            if (conversations is not null) json.WriteNumber("conversations", conversations.Value);
        });

    /// <summary>The question put to the session on a turn.</summary>
    public void Question(string mode, int repeat, string task, int turn, string session, string text) =>
        Emit("question", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("session", session);
            json.WriteString("text", text);
        });

    /// <summary>
    /// One event of a turn's stream, as the session sent it: its type and its JSON, written as the JSON it is.
    /// </summary>
    public void Sse(string mode, int repeat, string task, int turn, string type, JsonElement data) =>
        Emit("sse", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("type", type);
            json.WritePropertyName("data");
            data.WriteTo(json);
        });

    /// <summary>
    /// A turn; <paramref name="failure"/> is the infrastructure failure that ended it (F18 step 5.1), left out when it
    /// ended on its own.
    /// </summary>
    public void Turn(string mode, int repeat, string task, int turn, string session, string question, string? answer,
        string? role,
        double seconds, TurnFacts? facts = null, TurnFailure? failure = null) =>
        Emit("turn", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("session", session);
            json.WriteString("question", question);
            WriteText(json, "answer", answer);
            WriteText(json, "role", role);
            if (facts is not null) WriteFacts(json, facts);
            json.WriteNumber("seconds", seconds);
            if (facts is not null) WriteText(json, "error", facts.Error);
            if (failure is not null) json.WriteString("failure", V1Words.Failure(failure.Value));
        });

    /// <summary>A conversation's result: the eval's scores, written as the JSON they are.</summary>
    public void Conversation(string mode, int repeat, string task, string? role, JsonElement scores, double seconds,
        IReadOnlyList<Grade>? grades = null, TrialOutcome? outcome = null, int? errors = null) =>
        Emit("conversation",
            json => ConversationLineFields.Write(json, mode, repeat, task, role, scores, seconds, grades, outcome,
                errors));

    /// <summary>
    /// The run's first line (F18 step 2a): the eval, its version, the commit, the scenarios it asked for (its split,
    /// step 5.4), its graders and the model each mode ran on.
    /// </summary>
    public void Begin(string eval, int evalVersion, string commit, IReadOnlyList<IGrader> graders,
        IReadOnlyList<ModelInfo> models,
        RunSplit split = RunSplit.Dev, string? rescoreOf = null, DatasetManifest? dataset = null) =>
        Emit("protocol",
            json => RunBeginLine.Write(json, eval, evalVersion, commit, graders, models, split, rescoreOf, dataset));

    /// <summary>
    /// A line of a stored run, written as it was (F18 step 5b): a re-score keeps the questions, events and turns it
    /// grades again.
    /// </summary>
    public void Copy(string line)
    {
        stdout.Write(line);
        stdout.Write('\n');
        stdout.Flush();
    }

    /// <summary>
    /// A mode's repeat is done (F18 step 5a). With <paramref name="rates"/> the line also carries the mode's pass@k and
    /// pass^k over the repeats so far (left out when null: no trial counted, never 0), the <paramref name="k"/> they
    /// are over, and how many trials were left out for the infrastructure or run twice.
    /// </summary>
    public void ModeDone(string mode, int repeat, PassShare? rates = null, int? k = null, int? infrastructure = null,
        int? retried = null) =>
        Emit("mode_done", json =>
        {
            json.WriteString("mode", mode);
            json.WriteNumber("repeat", repeat);
            if (rates?.PassAtK is { } passAtK) json.WriteNumber("pass_at_k", passAtK);
            if (rates?.PassHatK is { } passHatK) json.WriteNumber("pass_hat_k", passHatK);
            if (k is not null) json.WriteNumber("k", k.Value);
            if (infrastructure is not null) json.WriteNumber("infrastructure", infrastructure.Value);
            if (retried is not null) json.WriteNumber("retried", retried.Value);
        });

    /// <summary>One slice of a mode's roll-up (F18 step 5c), written after the mode's last <c>mode_done</c>.</summary>
    public void Summary(SummaryLine line) => Emit("summary", json => SummaryLineFields.Write(json, line));

    /// <summary>
    /// What the eval says about the turn, in the Python's order: the continuation, the calls, the hosts (the error
    /// follows the seconds).
    /// </summary>
    private static void WriteFacts(Utf8JsonWriter json, TurnFacts facts)
    {
        WriteText(json, "continued", facts.Continued);
        json.WriteNumber("puls_calls", facts.AgentCalls);
        json.WriteNumber("specialist_calls", facts.SpecialistCalls);
        json.WriteStartArray("hosts");
        foreach (var host in facts.Hosts) json.WriteStringValue(host);
        json.WriteEndArray();
    }

    /// <summary>The four fields that say which turn of which trial a line is about.</summary>
    private static void WriteTurnKey(Utf8JsonWriter json, string mode, int repeat, string task, int turn)
    {
        json.WriteString("mode", mode);
        json.WriteNumber("repeat", repeat);
        json.WriteString("task", task);
        json.WriteNumber("turn", turn);
    }

    /// <summary>A text field, written as <c>null</c> when there is none, so the line always has the field.</summary>
    internal static void WriteText(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }

    /// <summary>Writes one line: <c>@</c>, then the event's name, the time and the fields as JSON, all ASCII.</summary>
    private void Emit(string name, Action<Utf8JsonWriter> fields)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, Options))
        {
            json.WriteStartObject();
            json.WriteString("event", name);
            // Milliseconds, so the seconds have three decimals, as the Python's round(time.time(), 3) gives.
            json.WriteNumber("t", clock.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0);
            fields(json);
            json.WriteEndObject();
        }

        stdout.Write('@');
        stdout.Write(Encoding.ASCII.GetString(buffer.ToArray()));
        stdout.Write('\n');
        stdout.Flush();
    }
}
