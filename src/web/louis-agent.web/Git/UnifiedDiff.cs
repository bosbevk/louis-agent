using System.Text.RegularExpressions;

namespace LouisAgent.Web.Git;

/// <summary>One row of a diff as the viewers draw it: its kind (hunk, ctx, add, del, note), its old and new line numbers, its text.</summary>
public sealed record DiffRow(string Kind, string Old, string New, string Text);

/// <summary>A unified diff read into rows (W2b, from DiffViewer).</summary>
public static class UnifiedDiff
{
    private static readonly Regex HunkHeader = new(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@(.*)$");

    /// <summary>Unified diff to rows: hunk headers, context, additions and removals with their line numbers.</summary>
    public static (List<DiffRow> Rows, int Added, int Removed) Parse(string diff)
    {
        var rows = new List<DiffRow>();
        int oldLine = 0, newLine = 0, added = 0, removed = 0;
        bool inHunk = false;
        foreach (string raw in diff.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith("@@"))
            {
                inHunk = true;
                var match = HunkHeader.Match(raw);
                if (match.Success)
                {
                    oldLine = int.Parse(match.Groups[1].Value);
                    newLine = int.Parse(match.Groups[2].Value);
                }
                rows.Add(new DiffRow("hunk", "", "", raw));
            }
            else if (!inHunk)
            {
                // File headers (diff --git, index, ---/+++) are summarised in the title; binary diffs have no hunks.
                if (raw.StartsWith("Binary files")) rows.Add(new DiffRow("note", "", "", raw));
            }
            else if (raw.StartsWith('+'))
            {
                rows.Add(new DiffRow("add", "", (newLine++).ToString(), raw[1..]));
                added++;
            }
            else if (raw.StartsWith('-'))
            {
                rows.Add(new DiffRow("del", (oldLine++).ToString(), "", raw[1..]));
                removed++;
            }
            else if (raw.StartsWith('\\'))
            {
                rows.Add(new DiffRow("note", "", "", raw));
            }
            else if (raw.StartsWith(' '))
            {
                rows.Add(new DiffRow("ctx", (oldLine++).ToString(), (newLine++).ToString(), raw[1..]));
            }
        }

        return (rows, added, removed);
    }
}
