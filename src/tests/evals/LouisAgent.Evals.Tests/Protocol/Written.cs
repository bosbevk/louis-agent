using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// What a runner wrote, read back as the contract's events, so a test holds the records and never the JSON text.
/// </summary>
internal static class Written
{
    /// <summary>Every line in the sink.</summary>
    public static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Every line in the sink as its event; a line that is no event fails the test.</summary>
    public static IReadOnlyList<EvalEvent> Events(StringWriter sink) => Events(Lines(sink));

    /// <summary>Every line as its event; a line that is no event fails the test.</summary>
    public static IReadOnlyList<EvalEvent> Events(IEnumerable<string> lines) =>
    [
        .. lines.Select(line => EvalProtocolJson.Read(line) ?? throw new AssertionException($"Not an event: {line}"))
    ];
}
