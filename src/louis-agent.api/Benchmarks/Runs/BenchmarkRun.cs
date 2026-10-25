using System.Threading.Channels;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// One run of a check (F27): what was started, the lines the script printed so far, and how it ended. Listeners get
/// the lines so far and then each new one as it arrives (<see cref="BenchmarkRunner.EventsAsync"/>), so a page that
/// opens late sees the whole story.
/// </summary>
public sealed class BenchmarkRun(string id, string check, int repeat, BenchmarkRunOptions? options = null, string? rescoreOf = null, string? input = null)
{
    // The cook check forwards every stream event of its conversations as a line (the portal replays them as the chat),
    // a few thousand per conversation; a late listener needs the whole story.
    private const int MaxLines = 100_000;

    private readonly Lock _lock = new();
    private readonly List<string> _lines = [];
    private readonly List<Channel<string>> _listeners = [];

    public string Id { get; } = id;
    public string Check { get; } = check;

    public int Repeat { get; } = repeat;

    /// <summary>How the run handles the web (F18 step 5.5): recorded, or a replay of another run. None for a run that asks for nothing.</summary>
    public BenchmarkRunOptions Options { get; } = options ?? BenchmarkRunOptions.None;

    /// <summary>The stored run this one re-scores (F18 step 5b.2); null for a run that asks the model.</summary>
    public string? RescoreOf { get; } = rescoreOf;

    /// <summary>What the process reads on stdin: the stored run's export, for a re-score; null for any other run.</summary>
    internal string? Input { get; } = input;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; private set; }
    public BenchmarkRunStatus Status { get; private set; } = BenchmarkRunStatus.Running;
    public int? ExitCode { get; private set; }

    /// <summary>The lines printed so far (the last <see cref="MaxLines"/>).</summary>
    public IReadOnlyList<string> Lines
    {
        get { lock (_lock) return _lines.ToArray(); }
    }

    internal void Add(string line)
    {
        lock (_lock)
        {
            if (_lines.Count >= MaxLines) _lines.RemoveAt(0);
            _lines.Add(line);
            foreach (var listener in _listeners) listener.Writer.TryWrite(line);
        }
    }

    internal void Complete(int exitCode)
    {
        lock (_lock)
        {
            if (Status != BenchmarkRunStatus.Running) return;
            ExitCode = exitCode;
            Status = exitCode == 0 ? BenchmarkRunStatus.Done : BenchmarkRunStatus.Failed;
            EndedAt = DateTimeOffset.UtcNow;
            foreach (var listener in _listeners) listener.Writer.TryComplete();
            _listeners.Clear();
        }
    }

    /// <summary>The lines so far, and a channel for the rest (already completed when the run has ended).</summary>
    internal (string[] SoFar, ChannelReader<string> Live) Listen()
    {
        lock (_lock)
        {
            var channel = Channel.CreateUnbounded<string>();
            if (Status == BenchmarkRunStatus.Running) _listeners.Add(channel);
            else channel.Writer.TryComplete();
            return (_lines.ToArray(), channel.Reader);
        }
    }
}
