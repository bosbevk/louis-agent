using System.Text.Json;
using System.Text.Json.Serialization;

namespace louis_agent.orchestrator;

/// <summary>One exception reported by the service (the shape an error tracker such as Exceptionless would give).</summary>
internal sealed record ErrorEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("arguments")] string Arguments,
    [property: JsonPropertyName("exception_type")] string ExceptionType,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("stack_trace")] string? StackTrace);

/// <summary>
/// Reads the service's error log and remembers which events were handled, so each exception is triaged once even
/// across restarts.
/// </summary>
internal sealed class ErrorFeed(string errorLogPath, string stateDirectory)
{
    private string ProcessedPath => Path.Combine(stateDirectory, "processed.txt");
    private readonly Dictionary<string, ErrorEvent> _seen = new();

    public IReadOnlyList<ErrorEvent> ReadNew()
    {
        if (!File.Exists(errorLogPath)) return [];

        var processed = File.Exists(ProcessedPath) ? File.ReadAllLines(ProcessedPath).ToHashSet() : [];
        var events = new List<ErrorEvent>();
        foreach (string line in File.ReadLines(errorLogPath).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            ErrorEvent? e;
            try { e = JsonSerializer.Deserialize<ErrorEvent>(line); }
            catch (JsonException) { continue; }
            // Ids end up in branch names and commit messages, so anything but a plain token is ignored.
            if (e is null || !IsPlainId(e.Id) || processed.Contains(e.Id)) continue;
            _seen[e.Id] = e;
            events.Add(e);
        }

        return events;
    }

    /// <summary>An event read by <see cref="ReadNew"/>; tools look events up by id so the model can't invent one.</summary>
    public ErrorEvent? Find(string id) => _seen.GetValueOrDefault(id);

    private static bool IsPlainId(string? id) =>
        id is { Length: > 0 and <= 64 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public void MarkProcessed(string id)
    {
        Directory.CreateDirectory(stateDirectory);
        File.AppendAllText(ProcessedPath, id + Environment.NewLine);
    }
}
