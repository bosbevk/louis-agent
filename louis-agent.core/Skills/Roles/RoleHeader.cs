namespace LouisAgent.Core.Skills.Roles;

/// <summary>
/// A role's own settings (F33 step 4; Louis: "we want the cook to have the tool always"), in a header at the top of its
/// file that the loader reads and strips, so the model never sees it:
/// <code>
/// ---
/// toolsets: shops?
/// ---
/// </code>
/// <c>toolsets</c> are loaded whenever the role is, beside what its parent passes; <c>?</c> marks one loaded only where
/// this instance has it. A file without the header declares nothing.
/// </summary>
public static class RoleHeader
{
    private const string Fence = "---";

    public static (IReadOnlyList<string> Toolsets, string Body) Split(string text)
    {
        string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string open = Fence + newline;
        if (!text.StartsWith(open, StringComparison.Ordinal)) return ([], text);

        string close = newline + Fence + newline;
        int end = text.IndexOf(close, open.Length - newline.Length, StringComparison.Ordinal);
        if (end < 0) return ([], text);

        var toolsets = new List<string>();
        foreach (string line in text[open.Length..end].Split(newline))
        {
            int colon = line.IndexOf(':');
            if (colon < 0 || !line[..colon].Trim().Equals("toolsets", StringComparison.OrdinalIgnoreCase)) continue;
            toolsets.AddRange(line[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        return (toolsets, text[(end + close.Length)..]);
    }
}
