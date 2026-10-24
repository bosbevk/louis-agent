using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

/// <summary>
/// The reference transcripts the conformance test reads (F18 step 4.2b), cut from the Python stub's own protocol lines: the reference
/// is a conversation as the protocol recorded it, and the known-bad one is the same conversation with an error event added to its first
/// turn, as the stream would send it.
/// </summary>
internal static class ReferenceTranscripts
{
    public static string[] PythonLines =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-stub.txt")).Where(line => line.StartsWith('@')).ToArray();

    public static IReadOnlyList<ConformanceCase> Of(EvalMode mode, params string[] tasks) =>
        tasks.Select(task => new ConformanceCase(task, mode,
            ProtocolTranscripts.Read(PythonLines, mode.Name, 1, task),
            ProtocolTranscripts.Read(WithError(PythonLines, mode.Name, task), mode.Name, 1, task))).ToList();

    /// <summary>The protocol's lines with an error event added after the conversation's first sse line.</summary>
    public static string[] WithError(string[] lines, string mode, string task)
    {
        int at = Array.FindIndex(lines, line => line.Contains($"\"mode\": \"{mode}\", \"repeat\": 1, \"task\": \"{task}\", \"turn\": 1, ")
            && line.Contains("\"event\": \"sse\""));
        if (at < 0) throw new InvalidOperationException($"no sse line for {mode} {task}");
        string error = $"@{{\"event\": \"sse\", \"t\": 0, \"mode\": \"{mode}\", \"repeat\": 1, \"task\": \"{task}\", \"turn\": 1, "
            + "\"type\": \"error\", \"data\": {\"type\": \"error\", \"error\": {\"type\": \"api_error\", \"message\": \"overloaded\"}}}";
        return lines.Take(at + 1).Append(error).Concat(lines.Skip(at + 1)).ToArray();
    }
}
