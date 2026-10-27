using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// The scan of the repeated-names ratchet on one source text (F18 step 7.5.3): the standalone string literals that are one of the wire names, minus the ones that are
/// not a toolset or a language by what they are. Apart from <see cref="RepeatedNameTests"/> so a test can feed it a line and see whether it is flagged.
/// </summary>
public static class NameScan
{
    public sealed record Hit(int Line, string Value, string Text);

    /// <summary>What comes right before the literal when it is not a name of ours: the executable a process starts, a key read from JSON or a dictionary.</summary>
    private static readonly Regex NotAToolsetBefore = new(@"(ProcessStartInfo|Process\.Start|ProcessRunner\.Run|GetProperty|TryGetProperty)\($|[\w\)]\[$", RegexOptions.Compiled);

    /// <summary>The code before the literal, when it makes the literal an executable (the dotnet host, python in its venv, the shell) or a role level's folder.</summary>
    private static readonly Regex NotAToolsetCode = new(
        @"\.FileName\b|candidates\.|CommonCommands|ResolveShell|_venvDirectory|\b(evalHost|DotNetHost)\s*=\s*$|\b(Specialists|Minions)Folder\s*=\s*$|Enum\.GetValues<\w+>\(\)\.Select\(\w+ => \w+\.Wire\(\)\)|@\*|\[""EVALS""\]\s*=\s*$|Has\.None\.(StartsWith|Contains)\($|Does\.Not\.Contain\($", RegexOptions.Compiled);

    private static readonly Regex BlockComment = new(@"/\*.*?\*/", RegexOptions.Compiled);

    public static IReadOnlyList<Hit> Flagged(string text, ISet<string> names, IReadOnlyCollection<string>? skippedClasses = null)
    {
        string[] lines = text.Split('\n');
        var hits = new List<Hit>();
        var bodies = DeclarationBodies.Spans(text, skippedClasses);
        foreach (var literal in StringLiterals.Regular(text).Where(l => names.Contains(l.Value) && !DeclarationBodies.Contains(bodies, l.Index)))
        {
            string line = lines[literal.Line - 1];
            string before = BlockComment.Replace(line[..literal.Column], "");
            if (!NotAToolsetCode.IsMatch(before) && !NotAToolsetBefore.IsMatch(before)) hits.Add(new Hit(literal.Line, literal.Value, line.Trim()));
        }
        return hits;
    }
}
