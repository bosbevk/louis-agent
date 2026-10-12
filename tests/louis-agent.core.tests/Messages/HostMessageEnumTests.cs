using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.Messages;

// docs/CLAUDE.md, "Messages and logging": the hosts keep their own message enums (core can't know their texts), in each
// project's Messages/ folder. Most hosts have no test project, so their enums are checked here from the source, like
// ArchitectureTests: every member has a [Description] template, and its braces are well-formed named holes.
public partial class HostMessageEnumTests
{
    private static IEnumerable<string> HostMessageFiles() =>
        new[] { "src", "tools" }
            .Select(root => Path.Combine(TestPaths.RepoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateDirectories(root))
            .Select(project => Path.Combine(project, "Messages"))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs"))
            .Order(StringComparer.Ordinal);

    [Test]
    public void TheHostsHaveMessageEnums()
    {
        Assert.That(HostMessageFiles().Select(Path.GetFileNameWithoutExtension),
            Is.SupersetOf(new[] { "AcpLogMessage", "AcpUserText", "AcpModelText", "ApiLogMessage", "McpServerLogMessage", "CliUserText",
                "OrchestratorToolMessage", "OrchestratorErrorMessage", "OrchestratorLogMessage" }));
    }

    [Test]
    public void EveryMember_HasATemplate_WithWellFormedHoles()
    {
        var problems = new List<string>();
        foreach (string file in HostMessageFiles())
        {
            string[] lines = File.ReadAllLines(file);
            int open = Array.FindIndex(lines, l => l.TrimStart().StartsWith("public enum "));
            if (open < 0) { problems.Add($"{file}: no enum"); continue; }

            string? template = null;
            foreach (string line in lines.Skip(open + 1))
            {
                if (Description().Match(line) is { Success: true } description)
                {
                    template = Regex.Unescape(description.Groups[1].Value);
                    continue;
                }

                if (Member().Match(line) is not { Success: true } member) continue;
                string name = $"{Path.GetFileNameWithoutExtension(file)}.{member.Groups[1].Value}";
                if (template is null) problems.Add($"{name} has no [Description]");
                else if (Hole().Replace(template, "").IndexOfAny(['{', '}']) >= 0) problems.Add($"{name}: \"{template}\"");
                template = null;
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    [GeneratedRegex("""^\s*\[Description\("((?:[^"\\]|\\.)*)"\)\]\s*$""")]
    private static partial Regex Description();

    [GeneratedRegex(@"^\s*(\w+),\s*$")]
    private static partial Regex Member();

    [GeneratedRegex(@"\{[A-Za-z][A-Za-z0-9]*\}")]
    private static partial Regex Hole();
}