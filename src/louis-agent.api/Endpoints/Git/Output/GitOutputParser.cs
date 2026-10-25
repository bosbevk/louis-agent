using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Git.Output;

/// <summary>Git's formats, parsed: status, commits, and a diff with the content of secret files dropped.</summary>
internal static partial class GitOutputParser
{
    internal const int MaxDiffChars = 500_000;
    internal const char Field = '\x1f', Record = '\x1e';

    // hash, short hash, author, author date, subject, refs, parents
    internal const string CommitFormat = "%H%x1f%h%x1f%an%x1f%aI%x1f%s%x1f%D%x1f%P";

    /// <summary>Parses `git status --porcelain=v1 -z`: "XY path", with renames followed by their original path.</summary>
    internal static List<GitFileStatus> ParseStatus(string output)
    {
        var files = new List<GitFileStatus>();
        var entries = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            if (entry.Length < 4) continue;
            char index = entry[0], workTree = entry[1];
            string path = entry[3..];
            string? oldPath = index is 'R' or 'C' && i + 1 < entries.Length ? entries[++i] : null;
            files.Add(new GitFileStatus(path, oldPath, index.ToString(), workTree.ToString()));
        }

        return files;
    }

    internal static List<GitCommitInfo> ParseCommits(string output) =>
        output.Split(Record).Select(r => r.Trim('\n', '\r')).Where(r => r.Length > 0).Select(ParseCommit).ToList();

    internal static GitCommitInfo ParseCommit(string record)
    {
        string[] f = record.Split(Field);
        string Get(int i) => i < f.Length ? f[i].Trim() : "";
        var refs = Get(5).Split(", ", StringSplitOptions.RemoveEmptyEntries).Select(r => r.Replace("HEAD -> ", "")).Where(r => r != "HEAD").ToList();
        var parents = Get(6).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return new GitCommitInfo(Get(0), Get(1), Get(2), Get(3), Get(4), refs, parents);
    }

    /// <summary>Drops the content of secret files from a multi-file patch, and caps its size.</summary>
    internal static (string Diff, bool Truncated) SafeDiff(string patch)
    {
        var result = new StringBuilder();
        foreach (string section in Regex.Split(patch, "(?=^diff --git )", RegexOptions.Multiline))
        {
            var header = DiffHeader().Match(section);
            if (!header.Success || !SensitiveFiles.IsSensitive(header.Groups[1].Value))
            {
                result.Append(section);
                continue;
            }

            // Keep the "diff --git" line so the file is still listed, but none of its content.
            int end = section.IndexOf('\n');
            result.Append(end < 0 ? section + "\n" : section[..(end + 1)]).Append(MessageText.Format(ApiUserText.SecretDiffBinary));
        }

        string text = result.ToString();
        return text.Length > MaxDiffChars ? (text[..MaxDiffChars], true) : (text, false);
    }

    [GeneratedRegex("^diff --git a/.+? b/(.+)$", RegexOptions.Multiline)]
    private static partial Regex DiffHeader();

    [GeneratedRegex("^[0-9a-fA-F]{7,40}$")]
    internal static partial Regex CommitHash();
}
