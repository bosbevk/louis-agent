namespace LouisAgent.Core.Tools.Documents;

using System.Text;

/// <summary>The table ReadTable shows (S, CL15, from WorkspaceTools): a header, the separator, one row per line.</summary>
internal static class MarkdownTable
{
    /// <summary>Every row padded or cut to <paramref name="columns"/> cells, a pipe in a cell escaped, a line break made a space.</summary>
    public static string Render(string[] header, IReadOnlyList<string[]> rows, int columns)
    {
        static string Line(IEnumerable<string> cells, int width) =>
            "| " + string.Join(" | ", cells.Concat(Enumerable.Repeat("", width)).Take(width).Select(c => c.Replace("|", "\\|").ReplaceLineEndings(" "))) + " |";
        var text = new StringBuilder();
        text.AppendLine(Line(header, columns));
        text.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", columns)));
        foreach (string[] row in rows) text.AppendLine(Line(row, columns));
        return text.ToString();
    }
}
