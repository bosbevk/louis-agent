using LouisAgent.Diagrams.Sources;

namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>Finds a doc's Mermaid flowcharts, to read each once into a flow beside it (CL60.2).</summary>
public static class MermaidImport
{
    /// <summary>The first words of a Mermaid flowchart; a block that starts otherwise is another kind.</summary>
    private static readonly string[] Flowcharts = ["flowchart", "graph "];

    /// <summary>
    /// Writes each flowchart in the doc as a JSON in the <c>diagrams</c> folder beside it, named for the doc and its
    /// heading; the doc itself is left for a person to edit (ADR 0052).
    /// </summary>
    public static IReadOnlyList<MermaidImported> Write(string docPath)
    {
        string doc = File.ReadAllText(docPath);
        string name = Path.GetFileNameWithoutExtension(docPath);
        string folder = Path.Combine(Path.GetDirectoryName(docPath)!, DiagramSync.Folder);
        // The doc's own title, without the "— design" a project doc's carries.
        string docTitle = doc.ReplaceLineEndings("\n").Split('\n').FirstOrDefault(line => line.StartsWith("# "))?[2..]
            .Split(" — ")[0].Trim() ?? name;

        var taken = new HashSet<string>(StringComparer.Ordinal);
        var imported = new List<MermaidImported>();
        foreach (var block in Blocks(doc))
        {
            string title = block.Heading.Length == 0 || block.Heading == docTitle
                ? docTitle
                : $"{docTitle}: {block.Heading}";
            string file = Unique($"{name}-{Slug(block.Heading)}".TrimEnd('-'), taken);
            string json = Path.Combine(folder, $"{file}.json");
            Directory.CreateDirectory(folder);
            File.WriteAllText(json, DiagramWriter.Write(MermaidFlowReader.Read(block.Body, title)));
            imported.Add(new MermaidImported(json, block, $"![{title}]({DiagramSync.Folder}/{file}.svg)"));
        }

        return imported;
    }

    /// <summary>Words as a file's name: lower case, a dash for each run of anything but letters and digits.</summary>
    private static string Slug(string words) => string.Join('-',
        new string([.. words.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ')])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The name, or with -2, -3 after it when the doc already used it.</summary>
    private static string Unique(string name, HashSet<string> taken)
    {
        string unique = name;
        for (int next = 2; !taken.Add(unique); next++) unique = $"{name}-{next}";
        return unique;
    }

    /// <summary>Every flowchart in the doc, with its lines and the heading above it.</summary>
    public static IReadOnlyList<MermaidBlock> Blocks(string doc)
    {
        var lines = doc.ReplaceLineEndings("\n").Split('\n');
        var blocks = new List<MermaidBlock>();
        string heading = "";
        for (int at = 0; at < lines.Length; at++)
        {
            string line = lines[at].Trim();
            if (line.StartsWith('#')) heading = line.TrimStart('#').Trim();
            if (!line.StartsWith("```", StringComparison.Ordinal)) continue;

            // A fence runs to the next; only a mermaid flowchart's is read, and a heading inside any fence is none.
            int end = Array.FindIndex(lines, at + 1, next => next.Trim().StartsWith("```", StringComparison.Ordinal));
            if (end < 0) break;
            string body = string.Join('\n', lines[(at + 1)..end]);
            if (line == "```mermaid" && Flowcharts.Any(body.TrimStart().StartsWith))
                blocks.Add(new MermaidBlock(at + 1, end + 1, body.Trim(), heading));
            at = end;
        }

        return blocks;
    }
}
