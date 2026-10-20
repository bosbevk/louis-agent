using System.Text;
using System.Threading.Channels;

namespace LouisAgent.AcpServer.Tests;

/// <summary>
/// Rider's side of the pipe, scripted: lines the test sends arrive on the server's stdin one by one, and every line the
/// server writes to stdout is collected, so the test can wait for a reply before sending what depends on it (the
/// session id).
/// </summary>
internal sealed class ScriptedConsole
{
    public ScriptedInput Input { get; } = new();
    public LineCollector Output { get; } = new();

    public void Send(string line) => Input.Send(line);
    public void End() => Input.End();

    /// <summary>Waits until the server has written <paramref name="count"/> lines.</summary>
    public async Task<IReadOnlyList<string>> LinesAsync(int count)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (Output.Lines.Count < count && DateTime.UtcNow < deadline) await Task.Delay(10);
        Assert.That(Output.Lines, Has.Count.GreaterThanOrEqualTo(count), "the server didn't answer in time");
        return Output.Lines;
    }

    internal sealed class ScriptedInput : TextReader
    {
        private readonly Channel<string> _lines = Channel.CreateUnbounded<string>();

        public void Send(string line) => _lines.Writer.TryWrite(line);
        public void End() => _lines.Writer.Complete();

        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            while (await _lines.Reader.WaitToReadAsync(cancellationToken))
                if (_lines.Reader.TryRead(out string? line)) return line;
            return null;
        }

        public override Task<string?> ReadLineAsync() => ReadLineAsync(CancellationToken.None).AsTask();
        public override string? ReadLine() => ReadLineAsync().GetAwaiter().GetResult();
    }

    internal sealed class LineCollector : TextWriter
    {
        private readonly StringBuilder _current = new();
        private readonly List<string> _lines = [];
        private readonly Lock _lock = new();

        public override Encoding Encoding => Encoding.UTF8;

        public IReadOnlyList<string> Lines { get { lock (_lock) return _lines.ToList(); } }

        public override void Write(char value)
        {
            lock (_lock)
            {
                if (value == '\n') { _lines.Add(_current.ToString().TrimEnd('\r')); _current.Clear(); }
                else _current.Append(value);
            }
        }
    }
}
