using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Choices;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Export;

/// <summary>
/// Writes an exported run one event at a time, each line an <c>@</c> and one JSON object in ASCII, just as the harness
/// writes them. The export walks the store and calls it.
/// </summary>
internal sealed class ProtocolLines(DateTimeOffset start)
{
    private static readonly JsonWriterOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin)
    };

    private readonly StringBuilder _text = new();
    private double _t = Seconds(start);

    /// <summary>Everything written so far, as one text.</summary>
    public override string ToString() => _text.ToString();

    /// <summary>The opening line: the protocol version, the eval, its graders and each mode's model.</summary>
    public void Protocol(Run run) => Emit("protocol", json =>
    {
        json.WriteNumber("version", BenchmarkProtocol.Version);
        if (run.Eval is null) return;
        json.WriteString("eval", run.Eval);
        if (run.EvalVersion is { } version) json.WriteNumber("eval_version", version);
        if (run.Commit is { } commit) json.WriteString("commit", commit);
        if (run.Split is { } split) json.WriteString("split", split.Wire());
        if (run.RescoreOf is { } rescoreOf) json.WriteString("rescore_of", rescoreOf);
        json.WriteStartArray("graders");
        foreach (var grader in run.Graders ?? [])
        {
            json.WriteStartObject();
            json.WriteString("name", grader.Name);
            json.WriteNumber("version", grader.Version);
            json.WriteString("kind", grader.Kind.Wire());
            if (grader.Question is { } question) json.WriteString("question", question);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteStartArray("models");
        foreach (var use in run.Models ?? [])
        {
            json.WriteStartObject();
            json.WriteString("mode", use.Mode);
            json.WriteString("level", use.Level.Wire());
            Text(json, "role", use.Role);
            Text(json, "model", use.Model);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    });

    /// <summary>The start of a mode's repeat: its toolsets, role, model and how many conversations follow.</summary>
    public void Mode(Mode mode, int conversations) => Emit("mode", json =>
    {
        json.WriteString("mode", mode.Name);
        json.WriteNumber("repeat", mode.Repeat);
        json.WriteStartArray("toolsets");
        foreach (string toolset in mode.Toolsets) json.WriteStringValue(toolset);
        json.WriteEndArray();
        Text(json, "role", mode.Role);
        Text(json, "llm", mode.Llm);
        json.WriteNumber("conversations", conversations);
    });

    /// <summary>The question a turn asked, before its stream events.</summary>
    public void Question(Turn turn) => Emit("question", json =>
    {
        Key(json, turn.Key);
        json.WriteString("session", turn.Session);
        json.WriteString("text", turn.Question);
    });

    /// <summary>One stream event of a turn. Its time moves the clock on for the lines after it.</summary>
    public void Sse(BenchmarkProtocol.TurnKey key, StreamEvent streamEvent)
    {
        if (streamEvent.At is { } at) _t = Seconds(at);
        Emit("sse", json =>
        {
            Key(json, key);
            json.WriteString("type", streamEvent.Type);
            json.WritePropertyName("data");
            streamEvent.Data.WriteTo(json);
        });
    }

    /// <summary>A finished turn: the answer, who answered, the calls it made, its time and any error.</summary>
    public void Turn(Turn turn) => Emit("turn", json =>
    {
        Key(json, turn.Key);
        json.WriteString("session", turn.Session);
        json.WriteString("question", turn.Question);
        Text(json, "answer", turn.Answer);
        Text(json, "role", turn.Role);
        Text(json, "continued", turn.Continued);
        json.WriteNumber("puls_calls", turn.PulsCalls);
        json.WriteNumber("specialist_calls", turn.SpecialistCalls);
        json.WriteStartArray("hosts");
        foreach (string host in turn.Hosts) json.WriteStringValue(host);
        json.WriteEndArray();
        json.WriteNumber("seconds", turn.Seconds);
        Text(json, "error", turn.Error);
        if (turn.Failure is { } failure) json.WriteString("failure", failure.Wire());
    });

    /// <summary>A finished conversation with its scores, each grader's grade and the outcome.</summary>
    public void Conversation(Conversation conversation) => Emit("conversation", json =>
    {
        json.WriteString("mode", conversation.Key.Mode);
        json.WriteNumber("repeat", conversation.Key.Repeat);
        json.WriteString("task", conversation.Key.Task);
        Text(json, "role", conversation.Role);
        json.WritePropertyName("scores");
        conversation.Scores.WriteTo(json);
        json.WriteNumber("errors", conversation.Errors);
        json.WriteNumber("seconds", conversation.Seconds);
        if (conversation.Grades is { } grades)
        {
            json.WriteStartArray("grades");
            foreach (var grade in grades)
            {
                json.WriteStartObject();
                json.WriteString("grader", grade.Grader);
                json.WriteNumber("version", grade.Version);
                json.WriteNumber("score", grade.Score);
                json.WriteString("outcome", grade.Outcome.Wire());
                json.WriteString("reason", grade.Reason);
                if (grade.JudgeModel is not null) json.WriteString("judge_model", grade.JudgeModel);
                if (grade.JudgePromptHash is not null) json.WriteString("judge_prompt_hash", grade.JudgePromptHash);
                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        if (conversation.Outcome is { } outcome) json.WriteString("outcome", outcome.Wire());
    });

    /// <summary>A line in the runner's own shape, for the summary export to fill in.</summary>
    internal void Line(string name, Action<Utf8JsonWriter> fields) => Emit(name, fields);

    /// <summary>The end of one repeat of a mode, with its pass rates when the runner worked them out.</summary>
    public void ModeDone(Mode mode) => Emit("mode_done", json =>
    {
        json.WriteString("mode", mode.Name);
        json.WriteNumber("repeat", mode.Repeat);
        if (mode.Rates is not { } rates) return;
        // The rates as the runner wrote them: a rate no trial made is left out, never written as 0.
        if (rates.PassAtK is { } passAtK) json.WriteNumber("pass_at_k", passAtK);
        if (rates.PassHatK is { } passHatK) json.WriteNumber("pass_hat_k", passHatK);
        json.WriteNumber("k", rates.K);
        json.WriteNumber("infrastructure", rates.Infrastructure);
        json.WriteNumber("retried", rates.Retried);
    });

    /// <summary>Which turn a line is about: its mode, repeat, task and turn number.</summary>
    private static void Key(Utf8JsonWriter json, BenchmarkProtocol.TurnKey key)
    {
        json.WriteString("mode", key.Mode);
        json.WriteNumber("repeat", key.Repeat);
        json.WriteString("task", key.Task);
        json.WriteNumber("turn", key.Turn);
    }

    /// <summary>A text field that is written as null when it's missing, as the harness does.</summary>
    private static void Text(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }

    /// <summary>A moment as Unix seconds with milliseconds, the way the harness stamps its lines.</summary>
    private static double Seconds(DateTimeOffset at) => at.ToUnixTimeMilliseconds() / 1000.0;

    /// <summary>Writes one line: <c>@</c>, then the event's name, time and fields as one JSON object.</summary>
    private void Emit(string name, Action<Utf8JsonWriter> fields)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, Options))
        {
            json.WriteStartObject();
            json.WriteString("event", name);
            json.WriteNumber("t", _t);
            fields(json);
            json.WriteEndObject();
        }

        _text.Append('@').Append(Encoding.ASCII.GetString(buffer.ToArray())).Append('\n');
    }
}
