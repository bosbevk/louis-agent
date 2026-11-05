using System.Text.Json;

namespace LouisAgent.Orchestrator.Tools;

/// <summary>
/// The orchestrator's own jsonl files in its state folder (fixes, escalations, acknowledgements): one record a line,
/// added to, never rewritten.
/// </summary>
/// <param name="stateDirectory">The orchestrator's state folder; made when the first line is written.</param>
internal sealed class StateFiles(string stateDirectory)
{
    /// <summary>What louis-agent did with each fix it was asked for.</summary>
    public const string Fixes = "fixes.jsonl";

    /// <summary>The errors handed to a human team.</summary>
    public const string Escalations = "escalations.jsonl";

    /// <summary>The errors the runbooks call expected.</summary>
    public const string Acknowledged = "acknowledged.jsonl";

    /// <summary>Adds one record as a line of the file.</summary>
    public void Append<T>(string fileName, T record)
    {
        Directory.CreateDirectory(stateDirectory);
        string line = JsonSerializer.Serialize(record) + Environment.NewLine;
        File.AppendAllText(Path.Combine(stateDirectory, fileName), line);
    }
}
