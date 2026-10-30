using LouisAgent.Choices;
using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Finds the evals that are folders of files: every <c>eval.json</c> under the copied eval files is one, and its
/// path below the copy's root is where it sits in the repository, which its manual's path starts from.
/// </summary>
/// <remarks>
/// A scan, not a list (finding F): a new eval is a folder dropped in, with no line of C# to register it. Until the
/// cook moves (F49.2 step 8), the C# eval sets are found as before, beside it.
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
    /// Whether <c>EVALS=</c> chooses an eval of this identity: every eval when it is unset or empty, else only the
    /// evals of the identities it names.
    /// </summary>
    public static bool Chosen(string? setting, string identity) =>
        string.IsNullOrWhiteSpace(setting) ||
        EvalSetSelection.Parse(setting).Any(kind => kind.Wire() == identity);
}

/// <summary>An eval folder: where its copy is, and where it sits in the repository (forward slashes).</summary>
public sealed record EvalFolder(string Folder, string Source);
