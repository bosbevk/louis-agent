using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Export;

/// <summary>The lines of an exported run, written one event at a time (F18 step 5.2): <c>@</c> and one JSON object each, ASCII only, as the harness writes them. <see cref="BenchmarkProtocolExport"/> walks the store and calls it.</summary>
internal sealed class ProtocolLines(DateTimeOffset start)
{
    private static readonly JsonWriterOptions Options = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin) };

    private readonly StringBuilder _text = new();
    private double _t = Seconds(start);

    public override string ToString() => _text.ToString();

    public void Protocol(Run run) => Emit("protocol", json =>
    {
        json.WriteNumber("version", BenchmarkProtocol.Version);
        if (run.Eval is null) return;
        json.WriteString("eval", run.Eval);
        if (run.EvalVersion is { } version) json.WriteNumber("eval_version", version);
        if (run.Commit is { } commit) json.WriteString("commit", commit);
        if (run.Split is { } split) json.WriteString("split", BenchmarkRunSplitName.Of(split));
        if (run.RescoreOf is { } rescoreOf) json.WriteString("rescore_of", rescoreOf);
        json.WriteStartArray("graders");
        foreach (var grader in run.Graders ?? [])
        {
            json.WriteStartObject();
            json.WriteString("name", grader.Name);
            json.WriteNumber("version", grader.Version);
            json.WriteString("kind", grader.Kind.ToString());
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteStartArray("models");
        foreach (var use in run.Models ?? [])
        {
            json.WriteStartObject();
            json.WriteString("mode", use.Mode);
            json.WriteString("level", use.Level.ToString());
            Text(json, "role", use.Role);
            Text(json, "model", use.Model);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

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

    public void Question(Turn turn) => Emit("question", json =>
    {
        Key(json, turn.Key);
        json.WriteString("session", turn.Session);
        json.WriteString("text", turn.Question);
    });

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
        if (turn.Failure is { } failure) json.WriteString("failure", BenchmarkTurnFailureName.Of(failure));
    });

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
                json.WriteString("outcome", grade.Outcome.ToString());
                json.WriteString("reason", grade.Reason);
                if (grade.JudgeModel is not null) json.WriteString("judge_model", grade.JudgeModel);
                if (grade.JudgePromptHash is not null) json.WriteString("judge_prompt_hash", grade.JudgePromptHash);
                json.WriteEndObject();
            }
            json.WriteEndArray();
        }
        if (conversation.Outcome is { } outcome) json.WriteString("outcome", outcome.ToString());
    });

    /// <summary>A line of the runner's own shape, written by <see cref="SummaryExport"/>.</summary>
    internal void Line(string name, Action<Utf8JsonWriter> fields) => Emit(name, fields);

    public void ModeDone(Mode mode) => Emit("mode_done", json =>
    {
        json.WriteString("mode", mode.Name);
        json.WriteNumber("repeat", mode.Repeat);
        if (mode.Rates is not { } rates) return;
        // The rates as the runner wrote them (F18 step 5a): a rate no trial made is left out, never 0.
        if (rates.PassAtK is { } passAtK) json.WriteNumber("pass_at_k", passAtK);
        if (rates.PassHatK is { } passHatK) json.WriteNumber("pass_hat_k", passHatK);
        json.WriteNumber("k", rates.K);
        json.WriteNumber("infrastructure", rates.Infrastructure);
        json.WriteNumber("retried", rates.Retried);
    });

    private static void Key(Utf8JsonWriter json, BenchmarkProtocol.TurnKey key)
    {
        json.WriteString("mode", key.Mode);
        json.WriteNumber("repeat", key.Repeat);
        json.WriteString("task", key.Task);
        json.WriteNumber("turn", key.Turn);
    }

    private static void Text(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }

    private static double Seconds(DateTimeOffset at) => at.ToUnixTimeMilliseconds() / 1000.0;

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
