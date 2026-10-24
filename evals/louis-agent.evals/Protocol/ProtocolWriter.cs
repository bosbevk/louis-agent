using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The line protocol the eval writes to stdout (F31's version 1): <c>@</c> and one JSON object per event, with the event's
/// name, the time <c>t</c> in seconds from the clock, and the event's fields in the order the Python harness wrote them.
/// ASCII only: every other character is escaped, so any console can show the line and JSON reads it back as it was.
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
    /// The old first line, the version alone. Kept only for the parity test against the Python's first line; a run writes
    /// <see cref="Begin"/>, which carries the eval.
    /// </summary>
    public void Started() => Emit("protocol", json => json.WriteNumber("version", Version));

    public void Mode(string mode, int repeat, IReadOnlyList<string> toolsets, string? role, string? llm) =>
        Emit("mode", json =>
        {
            json.WriteString("mode", mode);
            json.WriteNumber("repeat", repeat);
            json.WriteStartArray("toolsets");
            foreach (var toolset in toolsets) json.WriteStringValue(toolset);
            json.WriteEndArray();
            WriteText(json, "role", role);
            WriteText(json, "llm", llm);
        });

    public void Question(string mode, int repeat, string task, int turn, string session, string text) =>
        Emit("question", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("session", session);
            json.WriteString("text", text);
        });

    /// <summary>One event of a turn's stream, as the session sent it: its type and its JSON, written as the JSON it is.</summary>
    public void Sse(string mode, int repeat, string task, int turn, string type, JsonElement data) =>
        Emit("sse", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("type", type);
            json.WritePropertyName("data");
            data.WriteTo(json);
        });

    public void Turn(string mode, int repeat, string task, int turn, string session, string question, string? answer, string? role,
        double seconds) =>
        Emit("turn", json =>
        {
            WriteTurnKey(json, mode, repeat, task, turn);
            json.WriteString("session", session);
            json.WriteString("question", question);
            WriteText(json, "answer", answer);
            WriteText(json, "role", role);
            json.WriteNumber("seconds", seconds);
        });

    /// <summary>A conversation's result: the eval's scores, written as the JSON they are.</summary>
    public void Conversation(string mode, int repeat, string task, string? role, JsonElement scores, double seconds,
        IReadOnlyList<Grade>? grades = null, TrialOutcome? outcome = null) =>
        Emit("conversation", json =>
        {
            json.WriteString("mode", mode);
            json.WriteNumber("repeat", repeat);
            json.WriteString("task", task);
            WriteText(json, "role", role);
            json.WritePropertyName("scores");
            scores.WriteTo(json);
            json.WriteNumber("seconds", seconds);
            if (grades is not null)
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
                    json.WriteEndObject();
                }
                json.WriteEndArray();
            }
            if (outcome is not null) json.WriteString("outcome", outcome.Value.ToString());
        });

    /// <summary>The run's first line (F18 step 2a): the eval, its version, the commit, its graders and the model each mode ran on.</summary>
    public void Begin(string eval, int evalVersion, string commit, IReadOnlyList<IGrader> graders, IReadOnlyList<ModelUse> models) =>
        Emit("protocol", json =>
        {
            json.WriteNumber("version", Version);
            json.WriteString("eval", eval);
            json.WriteNumber("eval_version", evalVersion);
            json.WriteString("commit", commit);
            json.WriteStartArray("graders");
            foreach (var grader in graders)
            {
                json.WriteStartObject();
                json.WriteString("name", grader.Name);
                json.WriteNumber("version", grader.Version);
                json.WriteString("kind", grader.Kind.ToString());
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteStartArray("models");
            foreach (var use in models)
            {
                json.WriteStartObject();
                json.WriteString("mode", use.Mode);
                json.WriteString("level", use.Level.ToString());
                WriteText(json, "role", use.Role);
                WriteText(json, "model", use.Model);
                json.WriteEndObject();
            }
            json.WriteEndArray();
        });

    public void ModeDone(string mode, int repeat) =>
        Emit("mode_done", json =>
        {
            json.WriteString("mode", mode);
            json.WriteNumber("repeat", repeat);
        });

    private static void WriteTurnKey(Utf8JsonWriter json, string mode, int repeat, string task, int turn)
    {
        json.WriteString("mode", mode);
        json.WriteNumber("repeat", repeat);
        json.WriteString("task", task);
        json.WriteNumber("turn", turn);
    }

    private static void WriteText(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }

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
