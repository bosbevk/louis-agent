namespace LouisAgent.Packs.Files.Web;

/// <summary>What the file viewer's buttons put in the chat's box, for the person to read and send.</summary>
public static class FilePrompts
{
    /// <summary>A review of a workspace file, read with the agent's tools.</summary>
    public static string Review(string path) =>
        $"Review `{path}` from the workspace. Read it with your tools, then list bugs, risks and improvements, most important first, with line numbers.";

    /// <summary>The start of a question about a file, for the person to finish.</summary>
    public static string Ask(string path) => $"About `{path}`: ";
}
