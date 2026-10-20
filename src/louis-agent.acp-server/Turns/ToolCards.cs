namespace LouisAgent.AcpServer.Turns;

using System.Globalization;

/// <summary>What a tool call looks like in the client's chat (S, CL15, from AcpServer): the card's kind and its output preview.</summary>
internal static class ToolCards
{
    // Tool output shown in the client's tool card; the model still receives the full result.
    private const int MaxToolOutputPreviewChars = 2_000;

    private static readonly (string Prefix, string Kind)[] ToolKindPrefixes =
    [
        ("Read", "read"), ("Get", "read"), ("List", "read"), ("Show", "read"),
        ("Search", "search"), ("Find", "search"), ("WebSearch", "search"),
        ("Delete", "delete"), ("Rename", "move"),
        ("Write", "edit"), ("Append", "edit"), ("Create", "edit"), ("Copy", "edit"), ("Update", "edit"),
        ("Fetch", "fetch"),
        ("DotNet", "execute"), ("Python", "execute"), ("PowerShell", "execute"), ("Bash", "execute"),
    ];

    /// <summary>ACP tool kind (picks the icon the client shows), guessed from the tool's name.</summary>
    internal static string Kind(string toolName) =>
        ToolKindPrefixes.FirstOrDefault(k => toolName.StartsWith(k.Prefix, StringComparison.Ordinal)).Kind ?? "other";

    /// <summary>Tool output shown in the client's tool card, cut with a count of what is left; the model still receives the full result.</summary>
    internal static string Preview(string output) =>
        output.Length > MaxToolOutputPreviewChars
            // Invariant, so the count reads the same on every machine ("1,234", not the server's locale).
            ? output[..MaxToolOutputPreviewChars] + $"\n... [{(output.Length - MaxToolOutputPreviewChars).ToString("N0", CultureInfo.InvariantCulture)} more chars]"
            : output;
}
