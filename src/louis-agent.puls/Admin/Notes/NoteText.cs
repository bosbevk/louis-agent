using System.Globalization;

namespace LouisAgent.Puls.Admin.Notes;

/// <summary>The words and numbers a notes panel shows.</summary>
public static class NoteText
{
    /// <summary>The longest note the form takes. The API enforces the same limit, and has the last word.</summary>
    public const int MaxLength = 2000;

    /// <summary>When a note was written, in the reader's own time.</summary>
    public static string When(DateTimeOffset at) => at.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>The hint in the empty box.</summary>
    public static string Placeholder(bool onAConversation) =>
        onAConversation ? "A note on this conversation: what you saw, what you think happened" : "A note on this run: what changed, what you think explains it";
}
