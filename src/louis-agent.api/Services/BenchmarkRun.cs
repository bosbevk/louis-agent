using System.Threading.Channels;

namespace LouisAgent.Api.Services;

/// <summary>
/// One run of a check (F27): what was started, the lines the script printed so far, and how it ended. Listeners get
/// the lines so far and then each new one as it arrives (<see cref="BenchmarkRunner.EventsAsync"/>), so a page that
/// opens late sees the whole story.
/// </summary>
public sealed class BenchmarkRun(string id, string check, string folder, int repeat)
{
    private const int MaxLines = 2_000;

    private readonly Lock _lock = new();
    private readonly List<string> _lines = [];
    private readonly List<Channel<string>> _listeners = [];

    public string Id { get; } = id;
    public string Check { get; } = check;

    /// <summary>The folder under <c>samples/</c> the run writes to.</summary>
    public string Folder { get; } = folder;

    public int Repeat { get; } = repeat;
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
