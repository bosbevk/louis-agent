using System.Security.Cryptography;
using System.Text;

namespace LouisAgent.Evals.Datasets.Manifest;

/// <summary>
/// The hash of a dataset's scenarios (F18 step 3a): SHA-256 over every file under <c>scenarios/</c>, in ordinal order of their
/// forward-slash paths. Each file adds its path and then its content, each followed by a NUL byte. Content is read as text and
/// CRLF is read as LF, so a Windows checkout (autocrlf) and Linux hash the same. <c>dataset.json</c> is not in it: the manifest
/// holds the hash.
/// </summary>
public static class DatasetHash
{
    private static readonly byte[] Nul = [0];

    public static string Of(string scenariosFolder)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (relative, full) in Files(scenariosFolder))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(relative));
            hash.AppendData(Nul);
            hash.AppendData(Encoding.UTF8.GetBytes(File.ReadAllText(full).Replace("\r\n", "\n")));
            hash.AppendData(Nul);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>
    /// Every file under the folder, as its forward-slash path from the folder and its full path, in ordinal path order. The loader
    /// reads these same files, so what is hashed is what is loaded.
    /// </summary>
    internal static IEnumerable<(string Relative, string Full)> Files(string folder)
    {
        if (!Directory.Exists(folder)) return [];
        string root = Path.GetFullPath(folder);
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(full => (Relative: Path.GetRelativePath(root, full).Replace('\\', '/'), Full: full))
            .OrderBy(file => file.Relative, StringComparer.Ordinal);
    }
}
