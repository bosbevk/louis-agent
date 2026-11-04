namespace LouisAgent.Web.Git;

/// <summary>
/// The reviews a viewer can ask the agent for (W2b, from the chat page): each one's prompt and the title of the chat it
/// gets, since a review gets its own chat so it doesn't mix with whatever the current chat is about.
/// </summary>
public static class ReviewActions
{
    /// <summary>A workspace file, read with the agent's tools.</summary>
    public static (string Prompt, string Title) File(string path) =>
        ($"Review `{path}` from the workspace. Read it with your tools, then list bugs, risks and improvements, most important first, with line numbers.",
         $"Review {FileName(path)}");

    /// <summary>One file's staged or unstaged changes, or a new untracked file, read with the git tools.</summary>
    public static (string Prompt, string Title) Diff(string path, bool staged, bool untracked)
    {
        string which = untracked ? "new, untracked file" : staged ? "staged changes to" : "uncommitted changes to";
        return ($"Review the {which} `{path}`. Use your git tools to read the diff (and the file if you need context), then list bugs, " +
                "risks and improvements in the changed lines, most important first.",
                $"Review changes {FileName(path)}");
    }

    /// <summary>Everything uncommitted: the status, both diffs, and whether it looks ready to commit.</summary>
    public static (string Prompt, string Title) AllChanges() =>
        ("Review all my uncommitted changes. Use your git tools for the status and the staged and unstaged diffs, then for each " +
         "file list bugs, risks and improvements in the changed lines, most important first, and say whether it looks ready to commit.",
         "Review all changes");

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
