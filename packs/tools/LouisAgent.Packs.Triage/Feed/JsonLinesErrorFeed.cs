using System.Text.Json;

namespace LouisAgent.Packs.Triage.Feed;

/// <summary>
/// Reads a service's error log, one JSON line per error, and remembers which were handled in a file beside its
/// state, so each error is handed out once even across restarts.
/// </summary>
/// <remarks>
/// The orchestrator's feed, moved into the pack (F106.3.1) behind <see cref="IErrorFeed"/>. An id that isn't a
/// plain token is ignored, since ids end up in branch names and commit messages; a line that isn't JSON is skipped,
/// so one bad line never stops the rest.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class JsonLinesErrorFeed(string errorLogPath, string stateDirectory) : IErrorFeed
{
    private readonly Dictionary<string, ServiceError> _seen = new();

    private string HandledPath => Path.Combine(stateDirectory, "processed.txt");

    /// <inheritdoc />
    public IReadOnlyList<ServiceError> ReadNew()
    {
        if (!File.Exists(errorLogPath)) return [];

        var handled = File.Exists(HandledPath) ? File.ReadAllLines(HandledPath).ToHashSet() : [];
        var errors = new List<ServiceError>();
        foreach (string line in File.ReadLines(errorLogPath).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            ServiceError? error = Read(line);
            if (error is null || !PlainId.Is(error.Id) || handled.Contains(error.Id)) continue;
            _seen[error.Id] = error;
            errors.Add(error);
        }

        return errors;
    }

    /// <inheritdoc />
    public ServiceError? Find(string id) => _seen.GetValueOrDefault(id);

    /// <inheritdoc />
    public void MarkHandled(string id)
    {
        Directory.CreateDirectory(stateDirectory);
        File.AppendAllText(HandledPath, id + Environment.NewLine);
    }

    /// <summary>One line as an error, or nothing when it isn't JSON of that shape.</summary>
    private static ServiceError? Read(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<ServiceError>(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
