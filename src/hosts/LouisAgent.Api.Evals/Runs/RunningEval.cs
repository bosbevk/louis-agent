using System.Threading.Channels;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Start;

namespace LouisAgent.Api.Evals.Runs;

/// <summary>
/// One run of an eval: what was started, the lines the script printed so far, and how it ended. A page that opens
/// late still sees the whole story: it gets every line so far, then each new one as it comes.
/// </summary>
/// <remarks>
/// Observer, with a channel per listener. The run doesn't know who's watching; each listener gets its own queue, so a
/// slow page never holds up the script, and a late one is first handed the lines it missed.
/// </remarks>
public sealed class RunningEval(
    string id,
    string check,
    int repeat,
    EvalRunOptions? options = null,
    string? rescoreOf = null,
    string? input = null,
    EvalCalibration? calibration = null)
{
    // The cook check forwards every stream event of its conversations as a line (the portal replays them as the
    // chat), a few thousand per conversation; a late listener needs the whole story.
    private const int MaxLines = 100_000;

    private readonly Lock _lock = new();
    private readonly List<string> _lines = [];
    private readonly List<Channel<string>> _listeners = [];

    public string Id { get; } = id;

    /// <summary>The eval this run runs, by its name.</summary>
    public string Check { get; } = check;

    public int Repeat { get; } = repeat;

    /// <summary>How the run treats the web: recorded, a replay of another run, or nothing special.</summary>
    public EvalRunOptions Options { get; } = options ?? EvalRunOptions.None;

    /// <summary>The stored run this one grades again. Empty when the run asks the model afresh.</summary>
    public string? RescoreOf { get; } = rescoreOf;

    /// <summary>
    /// For a judge check: the stored run whose people's grades it checks, and the judge. Empty for an ordinary run.
    /// </summary>
    public EvalCalibration? Calibration { get; } = calibration;

    /// <summary>
    /// What the script is fed on its input: the stored run's export for a re-score, the people's grades for a judge
    /// check, nothing for any other run.
    /// </summary>
    internal string? Input { get; } = input;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; private set; }
    public EvalRunStatus Status { get; private set; } = EvalRunStatus.Running;
    public int? ExitCode { get; private set; }

    /// <summary>The lines printed so far (the last <see cref="MaxLines"/>).</summary>
    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (_lock) return _lines.ToArray();
        }
    }

    /// <summary>Keeps a line the script printed and passes it to everyone watching.</summary>
    internal void Add(string line)
    {
        lock (_lock)
        {
            // Past the cap, the oldest line makes room.
            if (_lines.Count >= MaxLines) _lines.RemoveAt(0);
            _lines.Add(line);
            foreach (var listener in _listeners) listener.Writer.TryWrite(line);
        }
    }

    /// <summary>
    /// Marks the run finished, done or failed by its exit code, and tells everyone watching there's nothing more.
    /// Only the first call counts.
    /// </summary>
    internal void Complete(int exitCode)
    {
        lock (_lock)
        {
            if (Status != EvalRunStatus.Running) return;

            ExitCode = exitCode;
            Status = exitCode == 0 ? EvalRunStatus.Done : EvalRunStatus.Failed;
            EndedAt = DateTimeOffset.UtcNow;

            // Closing each channel ends the listeners' loops; nobody needs to be kept after that.
            foreach (var listener in _listeners) listener.Writer.TryComplete();
            _listeners.Clear();
        }
    }

    /// <summary>The lines so far, and a channel for the rest (already closed when the run has ended).</summary>
    internal (string[] SoFar, ChannelReader<string> Live) Listen()
    {
        lock (_lock)
        {
            // Under the lock, so no line can slip in between the copy and the channel.
            var channel = Channel.CreateUnbounded<string>();
            if (Status == EvalRunStatus.Running) _listeners.Add(channel);
            else channel.Writer.TryComplete();
            return (_lines.ToArray(), channel.Reader);
        }
    }
}
