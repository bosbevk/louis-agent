namespace LouisAgent.Puls.Admin.Trends;

/// <summary>
/// A run the trend marks: the notes people wrote on it, and the lines where an agent's prompt changed since the run
/// before. Each marker reads its own list out on hover.
/// </summary>
public sealed record TrendMark(string Run, IReadOnlyList<string> Notes, IReadOnlyList<string> Changes)
{
    /// <summary>What the note marker says on hover: how many notes, then each one.</summary>
    /// <remarks>A switch on the count, one arm per case: none, one, more.</remarks>
    public string? NoteTip => Notes.Count switch
    {
        0 => null,
        1 => Listed("1 note", Notes),
        _ => Listed($"{Notes.Count} notes", Notes),
    };

    /// <summary>What the prompt-change marker says on hover: each line, with the agents that changed on it.</summary>
    public string? ChangeTip => Changes.Count == 0 ? null : Listed("A prompt changed", Changes);

    /// <summary>A heading, then one bullet per line, as a browser's tooltip shows it.</summary>
    private static string Listed(string heading, IEnumerable<string> items) =>
        string.Join("\n", items.Select(i => $"• {i}").Prepend(heading));
}
