using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LouisAgent.Core.Tests.Messages;

// docs/CLAUDE.md, "Messages and logging": the hosts keep their own message enums (core can't know their texts), in each
// project's Messages/ folder. Most hosts have no test project, so their enums are checked here from the source, like
// ArchitectureTests: every member has a [Description] template, and its braces are well-formed named holes.
/// <summary>Checks the message enums the hosts keep outside core, by reading their source as C#.</summary>
public partial class HostMessageEnumTests
{
    // CL15 step 4: a pack's and an identity's enums leave core too (packs/, identities/, a Messages/ folder at any depth).
    /// <summary>Every Messages/ folder's .cs files in the hosts, packs, identities and evals, outside bin and obj.</summary>
    private static IEnumerable<string> HostMessageFiles() =>
        new[] { "src", "tools", "packs", "identities", "evals" }
            .Select(root => Path.Combine(TestPaths.RepoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateDirectories(root, "Messages", SearchOption.AllDirectories))
            .Where(folder => !folder.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                             !folder.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs"))
            .Order(StringComparer.Ordinal);

    /// <summary>The hosts that have no test project of their own still have their message enums in the list.</summary>
    [Test]
    public void TheHostsHaveMessageEnums()
    {
        Assert.That(HostMessageFiles().Select(Path.GetFileNameWithoutExtension),
            Is.SupersetOf(new[]
            {
                "AcpLogMessage", "AcpUserText", "AcpModelText", "ApiLogMessage", "McpServerLogMessage", "CliUserText",
                "OrchestratorToolMessage", "OrchestratorErrorMessage", "OrchestratorLogMessage",
                "ApiModelText", "ApiUserText", "McpServerText", "CompositionErrorMessage", "ShopToolMessage",
                "ShopLogMessage",
                "EvalLogMessage", "GradeMessage", "EvalErrorMessage", "RunnerMessage", "StubMessage",
                "CommonGradeMessage", "ConformanceMessage",
                "ApiErrorMessage"
            }));
    }

    /// <summary>Every member of every message enum has a template, and its braces are only well-formed holes.</summary>
    [Test]
    public void EveryMember_HasATemplate_WithWellFormedHoles()
    {
        var problems = new List<string>();
        foreach (string file in HostMessageFiles())
        {
            // Read the file as C#, not line by line: a [Description] split over two lines still counts.
            var enums = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
                .DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();
            if (enums.Count == 0)
            {
                problems.Add($"{file}: no enum");
                continue;
            }

            foreach (EnumMemberDeclarationSyntax member in enums.SelectMany(e => e.Members))
            {
                string name = $"{Path.GetFileNameWithoutExtension(file)}.{member.Identifier.ValueText}";
                string? template = Template(member);
                if (template is null) problems.Add($"{name} has no [Description]");
                else if (Hole().Replace(template, "").IndexOfAny(['{', '}']) >= 0)
                    problems.Add($"{name}: \"{template}\"");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    /// <summary>The text of a member's [Description], or null when it has none.</summary>
    private static string? Template(EnumMemberDeclarationSyntax member)
    {
        // Take the first argument of the Description attribute. Its text is the template, with escapes already undone.
        return member.AttributeLists.SelectMany(list => list.Attributes)
            .Where(attribute => attribute.Name.ToString() is "Description" or "DescriptionAttribute")
            .Select(attribute =>
                attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax)
            .FirstOrDefault()?.Token.ValueText;
    }

    /// <summary>A named hole in a template: an opening brace, a name, and a closing brace.</summary>
    [GeneratedRegex(@"\{[A-Za-z][A-Za-z0-9]*\}")]
    private static partial Regex Hole();
}
