using System.ComponentModel;
using System.Text;
using LouisAgent.Composition.Guardrails.Workspace;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Docs.Messages;

namespace LouisAgent.Packs.Docs.Tools;

/// <summary>
/// The <c>docs</c> toolset: a model finds its way around a project's docs by listing a folder, searching for a
/// word and reading one doc, and never writes or leaves the docs root.
/// </summary>
/// <remarks>
/// The files pack's shape, narrowed: the same boundary guard keeps every path under the root, and only markdown and
/// plain text are read, so what the model gets is what a person reads.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class DocsTools
{
    /// <summary>A doc bigger than this is searched, never read whole.</summary>
    internal const long MostBytes = 1_000_000;

    /// <summary>The most matching lines a search returns.</summary>
    internal const int MostMatches = 100;

    private static readonly string[] DocExtensions = [".md", ".txt"];

    private readonly WorkspaceBoundary _boundary;
    private readonly string _root;

    /// <summary>Tools over the docs under <paramref name="root"/>.</summary>
    public DocsTools(string root)
    {
        _root = Path.GetFullPath(root);
        _boundary = new WorkspaceBoundary(_root);
    }

    /// <summary>What is in a folder of the docs: its folders, then its docs.</summary>
    [Description(
        "Lists the folders and the docs directly in one folder of the docs. Start with no folder for the top, where " +
        "README.md is the index of everything.")]
    public string ListDocs(
        [Description("A folder under the docs root, such as 'adr' or 'architecture'; empty for the top.")]
        string folder = "")
    {
        string path = Resolve(folder, out string? refused);
        if (refused is not null) return refused;
        if (!Directory.Exists(path)) return MessageText.Format(DocsToolMessage.FolderNotFound, new { Path = folder });

        // Folders first, then the docs, each as its path from the root, so the model can pass one straight back.
        var entries = Directory.EnumerateDirectories(path).Order(StringComparer.Ordinal).Select(d => Relative(d) + "/")
            .Concat(Directory.EnumerateFiles(path).Where(IsDoc).Order(StringComparer.Ordinal).Select(Relative))
            .ToList();
        return entries.Count == 0
            ? MessageText.Format(DocsToolMessage.FolderEmpty, new { Path = folder })
            : string.Join('\n', entries);
    }

    /// <summary>One doc, whole.</summary>
    [Description("Reads one doc of the docs, whole, as text. The path is the one ListDocs or SearchDocs showed.")]
    public string ReadDoc(
        [Description("The doc's path from the docs root, such as 'adr/0027-engine-identities-and-packs.md'.")]
        string path)
    {
        string full = Resolve(path, out string? refused);
        if (refused is not null) return refused;
        if (!File.Exists(full)) return MessageText.Format(DocsToolMessage.DocNotFound, new { Path = path });
        if (!IsDoc(full)) return MessageText.Format(DocsToolMessage.NotADoc, new { Path = path });
        if (new FileInfo(full).Length > MostBytes)
            return MessageText.Format(DocsToolMessage.DocTooLarge, new { Path = path });

        return File.ReadAllText(full, Encoding.UTF8);
    }

    /// <summary>Every line of the docs that holds the text, case ignored, as path:line: text.</summary>
    [Description(
        "Searches every doc for a word or phrase, case ignored, and returns each matching line as path:line: text, " +
        "so you know which doc to read next.")]
    public string SearchDocs(
        [Description("The text to find.")] string query,
        [Description("The most matching lines to return (1-100).")]
        int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(query)) return MessageText.Format(DocsToolMessage.QueryEmpty);
        maxResults = Math.Clamp(maxResults, 1, MostMatches);

        var matches = new List<string>();
        foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
                     .Where(IsDoc).Order(StringComparer.Ordinal))
        {
            if (new FileInfo(file).Length > MostBytes) continue;
            int lineNumber = 0;
            foreach (string line in File.ReadLines(file))
            {
                lineNumber++;
                if (!line.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                matches.Add($"{Relative(file)}:{lineNumber}: {line.Trim()}");
                if (matches.Count == maxResults) break;
            }

            if (matches.Count == maxResults) break;
        }

        return matches.Count == 0
            ? MessageText.Format(DocsToolMessage.NoMatches, new { Query = query })
            : string.Join('\n', matches);
    }

    /// <summary>A doc is markdown or plain text; anything else (an image, a film) is not read.</summary>
    private static bool IsDoc(string path) =>
        DocExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>A path as the model sees it: from the root, with forward slashes.</summary>
    private string Relative(string full) => Path.GetRelativePath(_root, full).Replace('\\', '/');

    /// <summary>
    /// The full path of a doc's path, or the refusal when it reaches outside the docs; the empty path is the root.
    /// </summary>
    private string Resolve(string relative, out string? refused)
    {
        refused = null;
        if (string.IsNullOrWhiteSpace(relative)) return _root;
        try
        {
            return _boundary.Resolve(relative);
        }
        catch (ArgumentException)
        {
            // The boundary says why in its own words; the model only needs to know it was the path.
            refused = MessageText.Format(DocsToolMessage.OutsideTheDocs, new { Path = relative });
            return _root;
        }
    }
}
