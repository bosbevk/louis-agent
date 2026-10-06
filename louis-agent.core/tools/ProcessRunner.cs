namespace louis_agent.core.tools;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Runs an external process without a shell: arguments are passed verbatim, stdout/stderr are captured
/// together, and on timeout the whole process tree is killed. Shared by the dotnet and python toolsets.
/// </summary>
internal static class ProcessRunner
{
    internal sealed record Result(int ExitCode, string Output, bool TimedOut);

    public static Result Run(
        string fileName,
        IEnumerable<string> args,
        string workingDirectory,
        TimeSpan timeout,
        string? standardInput = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);
            if (environment is not null)
                foreach (var (key, value) in environment) psi.Environment[key] = value;

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            object gate = new();
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (gate) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (gate) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!string.IsNullOrEmpty(standardInput)) process.StandardInput.Write(standardInput);
            process.StandardInput.Close();

            if (!process.WaitForExit(timeout))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                lock (gate) return new Result(-1, output.ToString(), TimedOut: true);
            }

            process.WaitForExit(); // flush async output handlers
            lock (gate) return new Result(process.ExitCode, output.ToString(), TimedOut: false);
        }
        catch (Exception ex)
        {
            return new Result(-1, $"Error running {fileName}: {ex.Message}", TimedOut: false);
        }
    }

    public static string Tail(string text, int maxChars = 3000) =>
        text.Length <= maxChars ? text.Trim() : "...\n" + text[^maxChars..].Trim();

    public static string Truncate(string text, int maxChars = 12_000) =>
        text.Length <= maxChars ? text : text[..maxChars] + $"\n... [truncated {text.Length - maxChars} chars]";
}
