namespace LouisAgent.Web.Shared.Text;

/// <summary>A tool call's input as the one line its summary shows (W2b, from BlocksView).</summary>
public static class ToolArgs
{
    /// <summary>The JSON on one line, without its outer braces, cut at eighty characters with an ellipsis.</summary>
    public static string Summarise(string json)
    {
        string flat = string.Join(" ", json.Split('\n', StringSplitOptions.TrimEntries)).Trim('{', '}', ' ');
        return flat.Length > 80 ? flat[..80] + "…" : flat;
    }
}
