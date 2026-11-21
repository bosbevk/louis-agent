using System.Security.Cryptography;
using System.Text;
using LouisAgent.Choices;

namespace LouisAgent.FilmExport.Films;

/// <summary>A fingerprint of what a film is made of, kept beside its export, so a stale export fails a test.</summary>
/// <remarks>
/// A test can't tell by looking at a WebP whether it still shows the film, and rendering one takes minutes. So the
/// export records a hash of the film's own files, and comparing two hashes is instant.
/// </remarks>
public static class FilmHash
{
    /// <summary>The film's hash today, the same on Windows and on CI however each checks the files out.</summary>
    public static string Of(FilmSource film, string repoRoot)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in film.Files)
        {
            string text = File.ReadAllText(Path.Combine(repoRoot, file)).ReplaceLineEndings("\n");
            // The name between files, so text moved from one file to the next still changes the hash.
            hash.AppendData(Encoding.UTF8.GetBytes($"{file}\0{text}\0"));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>The hash file's text with this film's line set, and the other films' lines kept.</summary>
    public static string Record(string? existing, FilmSource film, string hash)
    {
        var lines = Lines(existing);
        lines[film.Name.Wire()] = hash;
        return string.Concat(lines.OrderBy(line => line.Key, StringComparer.Ordinal)
            .Select(line => $"{line.Key} {line.Value}\n"));
    }

    /// <summary>The hash recorded for a film, or nothing if it has none yet.</summary>
    public static string? Read(string? existing, FilmName film) => Lines(existing).GetValueOrDefault(film.Wire());

    /// <summary>The file's lines, each a film's name and its hash.</summary>
    private static Dictionary<string, string> Lines(string? text) =>
        (text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(line => line.Split(' ', 2))
        .ToDictionary(parts => parts[0], parts => parts[1]);
}
