using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Catalog;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Evals.Files;

/// <summary>
/// The host's evals, found where its prompts are: every <c>.eval.json</c> in an agent's folder, read once at
/// start-up and held by its name, the file's name without its ending. A wrong file stops it at the first fault, so
/// the host never starts with an eval it can't run.
/// </summary>
/// <remarks>
/// It walks the same embedded files the catalogue already hands the prompt reader, which takes only markdown, so an
/// eval file travels with its agent and no prompt ever sees it. Whether a file's agent is there is asked of the
/// catalogue, never of a second list of names.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class EvalCatalog
{
    /// <summary>How an eval file's name ends.</summary>
    public const string Ending = ".eval.json";

    private readonly SortedDictionary<string, EvalFile> _evals;

    private EvalCatalog(SortedDictionary<string, EvalFile> evals) => _evals = evals;

    /// <summary>The evals' names, in order.</summary>
    public IReadOnlyList<string> Names => _evals.Keys.ToList();

    /// <summary>Every eval file among the catalogue's files, read and checked, or the first fault.</summary>
    public static EvalCatalog Read(AgentCatalog catalog)
    {
        var evals = new SortedDictionary<string, EvalFile>(StringComparer.Ordinal);
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (provider, path) in catalog.Prompts.SelectMany(provider =>
                     FilesOf(provider, "").Select(path => (provider, path))))
        {
            string name = Path.GetFileName(path)[..^Ending.Length];
            if (paths.TryGetValue(name, out string? first))
            {
                var usedTwice = new NameUsedTwiceHoles(name, first, path);
                throw new EvalFileException(path, MessageText.Format(CatalogMessage.NameUsedTwice, usedTwice));
            }

            evals[name] = ReadOne(catalog, provider, path);
            paths[name] = path;
        }

        return new EvalCatalog(evals);
    }

    /// <summary>The eval of this name, or null when the host has none.</summary>
    public EvalFile? Find(string name) => _evals.GetValueOrDefault(name);

    /// <summary>One file read with the eval file's reader, its agent checked against the catalogue's.</summary>
    private static EvalFile ReadOne(AgentCatalog catalog, IFileProvider provider, string path)
    {
        using var reader = new StreamReader(provider.GetFileInfo(path).CreateReadStream());
        EvalFile eval;
        try
        {
            eval = EvalFileReader.Read(reader.ReadToEnd());
        }
        catch (EvalFileException fault)
        {
            var refused = new FileRefusedHoles(path, fault.Message);
            throw new EvalFileException(path, MessageText.Format(CatalogMessage.FileRefused, refused), fault);
        }

        if (catalog.Identity(eval.Agent.Identity) is not null) return eval;

        var unknown = new AgentUnknownHoles(path, eval.Agent.Identity);
        throw new EvalFileException(path, MessageText.Format(CatalogMessage.AgentUnknown, unknown));
    }

    /// <summary>
    /// Every eval file a provider holds, at any depth, with forward slashes. The embedded provider's root is
    /// <c>/</c>, so the walk starts there.
    /// </summary>
    private static IEnumerable<string> FilesOf(IFileProvider provider, string folder) =>
        provider.GetDirectoryContents(folder.Length == 0 ? "/" : folder).SelectMany(file =>
        {
            string path = folder.Length == 0 ? file.Name : $"{folder}/{file.Name}";
            if (file.IsDirectory) return FilesOf(provider, path);
            return path.EndsWith(Ending, StringComparison.Ordinal) ? [path] : Enumerable.Empty<string>();
        });
}
