namespace LouisAgent.DocDiagrams.Tests.Docs;

/// <summary>
/// The SVG pictures a doc shows: each markdown link's target, read between <c>](</c> and the <c>)</c> after it,
/// outside code, since a how-to or a plan shows a link as an example there.
/// </summary>
/// <remarks>
/// Read in one place, since the rules on every doc's pictures and the rules on a project doc's both ask it.
/// </remarks>
public static class DocLinks
{
    /// <summary>The pictures a doc's text links, as written in the doc.</summary>
    public static IEnumerable<string> Pictures(string doc)
    {
        bool fenced = false;
        foreach (string line in doc.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) fenced = !fenced;
            if (fenced) continue;

            // The parts between backticks are inline code: every other part, from the second.
            string prose = string.Concat(line.Split('`').Where((_, part) => part % 2 == 0));
            foreach (string target in Targets(prose).Where(target => target.EndsWith(".svg", StringComparison.Ordinal)))
                yield return target;
        }
    }

    /// <summary>Each markdown link's target on a line of prose.</summary>
    private static IEnumerable<string> Targets(string prose)
    {
        for (int at = prose.IndexOf("](", StringComparison.Ordinal);
             at >= 0;
             at = prose.IndexOf("](", at + 2, StringComparison.Ordinal))
        {
            int end = prose.IndexOf(')', at + 2);
            if (end < 0) yield break;
            yield return prose[(at + 2)..end];
        }
    }
}
