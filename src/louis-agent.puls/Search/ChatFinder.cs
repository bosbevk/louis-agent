namespace LouisAgent.Puls.Search;

using LouisAgent.Web.Shared.Chats;

/// <summary>
/// The search over the chats saved in this browser (F20, Ctrl/Cmd K; W2b, from the ChatSearch component): their titles,
/// the user's messages and the answers. Nothing leaves the browser.
/// </summary>
public static class ChatFinder
{
    /// <summary>The chats shown while nothing is typed: the newest.</summary>
    private const int RecentCount = 8;

    /// <summary>The most a query shows.</summary>
    private const int MaxResults = 20;

    /// <summary>The chats that match, newest first, each with the line that matched (null when the title did).</summary>
    public static List<(Chat Chat, string? Snippet)> Find(IReadOnlyList<Chat> chats, string query)
    {
        string q = query.Trim();
        if (q.Length == 0) return chats.Take(RecentCount).Select(c => (c, (string?)null)).ToList();
        var found = new List<(Chat, string?)>();
        foreach (var chat in chats)
        {
            if (chat.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) { found.Add((chat, null)); continue; }
            string? line = chat.Turns
                .SelectMany(t => t.Blocks.Where(b => b.Kind == BlockKind.Text).Select(b => b.Text).Prepend(t.UserText))
                .Select(text => Snippet(text, q)).FirstOrDefault(s => s is not null);
            if (line is not null) found.Add((chat, line));
        }
        return found.Take(MaxResults).ToList();
    }

    /// <summary>About 80 characters around the first match in <paramref name="text"/>, or null when it doesn't match.</summary>
    public static string? Snippet(string text, string query)
    {
        int at = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;
        int start = Math.Max(0, at - 30);
        string piece = text.Substring(start, Math.Min(80, text.Length - start)).ReplaceLineEndings(" ");
        return (start > 0 ? "…" : "") + piece + (start + 80 < text.Length ? "…" : "");
    }
}
