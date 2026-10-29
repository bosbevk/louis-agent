using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// The same scan for a <c>.razor</c> file, which is markup around C# and can't be read as C#, so it keeps the plain
/// lexer and looks at the text before the string. A razor file holds markup only, so there are no enum or class bodies
/// to skip.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class RazorNameScan
{
    /// <summary>
    /// What comes right before the string when it is not a name of ours: the program a process starts, a key read from
    /// JSON or a dictionary.
    /// </summary>
    private static readonly Regex NotAToolsetBefore =
        new(@"(ProcessStartInfo|Process\.Start|ProcessRunner\.Run|GetProperty|TryGetProperty)\($|[\w\)]\[$",
            RegexOptions.Compiled);

    /// <summary>
    /// The code before the string, when it makes the string a program or a role level's folder, or a markup comment.
    /// </summary>
    private static readonly Regex NotAToolsetCode = new(
        @"\.FileName\b|candidates\.|CommonCommands|ResolveShell|_venvDirectory|\b(evalHost|DotNetHost)\s*=\s*$|\b(Specialists|Minions)Folder\s*=\s*$|Enum\.GetValues<\w+>\(\)\.Select\(\w+ => \w+\.Wire\(\)\)|@\*|\[""EVALS""\]\s*=\s*$|Has\.None\.(StartsWith|Contains)\($|Does\.Not\.Contain\($",
        RegexOptions.Compiled);

    /// <summary>
    /// A comment that opens and closes on the same line, which is cut out before the text is looked at.
    /// </summary>
    private static readonly Regex BlockComment = new(@"/\*.*?\*/", RegexOptions.Compiled);

    /// <summary>
    /// The plain strings of the file that spell one of the names and aren't excused, with their lines.
    /// </summary>
    public static IReadOnlyList<NameScan.Hit> Flagged(string text, ISet<string> names)
    {
        string[] lines = text.Split('\n');
        var hits = new List<NameScan.Hit>();
        foreach (var literal in StringLiterals.Regular(text).Where(l => names.Contains(l.Value)))
        {
            string line = lines[literal.Line - 1];
            string before = BlockComment.Replace(line[..literal.Column], "");
            if (!NotAToolsetCode.IsMatch(before) && !NotAToolsetBefore.IsMatch(before))
                hits.Add(new NameScan.Hit(literal.Line, literal.Value, line.Trim()));
        }

        return hits;
    }
}
