using LouisAgent.Core.Messages;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Finds the evals that are folders of files: every <c>eval.json</c> under the copied eval files is one, and its
/// path below the copy's root is where it sits in the repository, which its manual's path starts from. Then
/// <c>EVALS=</c> chooses among them by the identity each one proves.
/// </summary>
/// <remarks>
/// A scan, not a list (finding F): a new eval is a folder dropped in, with no line of C# to register it.
/// </remarks>
public static class EvalFolders
{
    /// <summary>The file every eval folder holds.</summary>
    private const string EvalFile = "eval.json";

    /// <summary>Every eval folder under <paramref name="root"/>, in the order of their paths.</summary>
    public static IReadOnlyList<EvalFolder> Find(string root) =>
        !Directory.Exists(root)
            ? []
            :
            [
                .. Directory.EnumerateFiles(root, EvalFile, SearchOption.AllDirectories)
                    .Select(file => Path.GetDirectoryName(file)!)
                    .Select(folder => new EvalFolder(folder,
                        Path.GetRelativePath(root, folder).Replace(Path.DirectorySeparatorChar, '/')))
                    .OrderBy(folder => folder.Source, StringComparer.Ordinal)
            ];

    /// <summary>
    /// The evals <c>EVALS=</c> asks for: with nothing set, all of them; otherwise those of the identities it names,
    /// separated by commas, case ignored. A name no eval proves is refused, naming the ones that do, so a typo runs
    /// nothing instead of everything.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public static IReadOnlyList<FileEval> Choose(IReadOnlyList<FileEval> evals, string? setting)
    {
        var names = (setting ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0) return evals;

        var proven = evals.Select(eval => eval.Identity).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var unknown = names.FirstOrDefault(name => !proven.Contains(name, StringComparer.OrdinalIgnoreCase));
        if (unknown is not null)
            throw new RunnerException(MessageText.Format(RunnerMessage.UnknownIdentity,
                new
                {
                    Name = unknown,
                    Supported = string.Join(", ", proven)
                }));

        return [.. evals.Where(eval => names.Contains(eval.Identity, StringComparer.OrdinalIgnoreCase))];
    }
}

/// <summary>An eval folder: where its copy is, and where it sits in the repository (forward slashes).</summary>
public sealed record EvalFolder(string Folder, string Source);
