namespace louis_agent.core;

using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>
/// Optional file logging under <see cref="config.AgentOptions.LogDirectory"/>: stderr is copied to one file per run
/// (containers run with --rm, so their own logs disappear on exit) and notable events are appended as JSON lines.
/// Everything is a no-op until <see cref="Initialize"/> is called with a directory.
/// </summary>
public static class AgentLog
{
    private static readonly object Gate = new();
    private static string? _directory;

    public static string? Directory => _directory;

    /// <summary>Starts copying stderr to {directory}/agent-{host}-{timestamp}.log. Safe to call more than once.</summary>
    public static void Initialize(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || _directory is not null) return;
        try
        {
            System.IO.Directory.CreateDirectory(directory);
            string host = (Assembly.GetEntryAssembly()?.GetName().Name ?? "agent").Replace("louis-agent.", "");
            string path = Path.Combine(directory, $"agent-{host}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
            var file = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
            {
                AutoFlush = true,
            };
            Console.SetError(TextWriter.Synchronized(new TeeWriter(Console.Error, file)));
            _directory = directory;
            Console.Error.WriteLine($"[INFO] Logging to {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] File logging disabled; could not use '{directory}': {ex.Message}");
        }
    }

    /// <summary>Appends one record to oversized-tool-results.jsonl, to spot tools that need narrower output.</summary>
    public static void RecordOversizedToolResult(
        string tool, string arguments, int chars, string? userRequest, string outcome, int returnedChars)
    {
        AppendJsonLine("oversized-tool-results.jsonl", new
        {
            timestamp = DateTimeOffset.Now,
            tool,
            arguments,
            chars,
            returnedChars,
            outcome,
            userRequest = userRequest is { Length: > 500 } ? userRequest[..500] + "..." : userRequest,
        });
    }

    /// <summary>Appends one record to truncated-tool-calls.jsonl: calls the model could not finish within the output limit.</summary>
    public static void RecordTruncatedToolCall(string tool, string arguments, int maxOutputTokens)
    {
        AppendJsonLine("truncated-tool-calls.jsonl", new { timestamp = DateTimeOffset.Now, tool, arguments, maxOutputTokens });
    }

    private static void AppendJsonLine(string fileName, object record)
    {
        if (_directory is null) return;
        try
        {
            string line = JsonSerializer.Serialize(record) + Environment.NewLine;
            lock (Gate) File.AppendAllText(Path.Combine(_directory, fileName), line, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] Could not write {fileName}: {ex.Message}");
        }
    }

    /// <summary>Writes to the original stderr and the log file; a failing file never breaks the console.</summary>
    private sealed class TeeWriter(TextWriter console, TextWriter file) : TextWriter
    {
        private bool _fileFailed;

        public override Encoding Encoding => console.Encoding;

        public override void Write(char value)
        {
            console.Write(value);
            ToFile(f => f.Write(value));
        }

        public override void Write(string? value)
        {
            console.Write(value);
            ToFile(f => f.Write(value));
        }

        public override void WriteLine(string? value)
        {
            console.WriteLine(value);
            ToFile(f => f.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {value}"));
        }

        public override void Flush()
        {
            console.Flush();
            ToFile(f => f.Flush());
        }

        private void ToFile(Action<TextWriter> write)
        {
            if (_fileFailed) return;
            try { write(file); }
            catch { _fileFailed = true; }
        }
    }
}
