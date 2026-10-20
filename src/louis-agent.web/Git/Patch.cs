using System.Text.RegularExpressions;

namespace LouisAgent.Web.Git;

/// <summary>One file's part of a patch: its path, its status (new file, deleted, renamed; null for a change), its rows and counts.</summary>
public sealed record PatchFile(string Path, string? Status, List<DiffRow> Rows, int Added, int Removed);

/// <summary>A multi-file patch (a commit or a branch) split per file (W2b, from PatchView).</summary>
public static class Patch
{
    private static readonly Regex FileStart = new("(?=^diff --git )", RegexOptions.Multiline);

    /// <summary>Splits "diff --git a/x b/y" sections and parses each one's hunks.</summary>
    public static List<PatchFile> Split(string patch)
    {
        var files = new List<PatchFile>();
        foreach (string section in FileStart.Split(patch.Replace("\r\n", "\n")).Where(s => s.StartsWith("diff --git ")))
        {
            string header = section[..(section.IndexOf('\n') is var end and >= 0 ? end : section.Length)];
            int b = header.LastIndexOf(" b/", StringComparison.Ordinal);
            string path = b >= 0 ? header[(b + 3)..] : header;
            string? status = section.Contains("\nnew file mode") ? "new file"
                : section.Contains("\ndeleted file mode") ? "deleted"
                : section.Contains("\nrename from") ? "renamed" : null;
            var (rows, added, removed) = UnifiedDiff.Parse(section);
            files.Add(new PatchFile(path, status, rows, added, removed));
        }

        return files;
    }
}
