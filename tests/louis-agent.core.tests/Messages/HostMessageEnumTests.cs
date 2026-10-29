using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LouisAgent.Core.Tests.Messages;

/// <summary>
/// Checks the message enums the hosts keep outside core, by reading their source as C#. The hosts keep their own
/// message enums (core can't know their texts), in each project's Messages/ folder; most have no test project, so
/// their enums are checked here, like ArchitectureTests: every member has a [Description] template, and its braces are
/// well-formed named holes.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public partial class HostMessageEnumTests
{
    /// <summary>
    /// Every Messages/ folder's .cs files in the hosts, packs, identities and evals, outside bin and obj (a pack's and
    /// an identity's enums left core in CL15).
    /// </summary>
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
                "AcpLogMessage", "AcpUserText", "AcpModelText", "ApiLogMessage", "McpServerLogMessage",
                "CliUserText", "OrchestratorToolMessage", "OrchestratorErrorMessage", "OrchestratorLogMessage",
                "ApiModelText", "ApiUserText", "McpServerText", "CompositionErrorMessage", "ShopToolMessage",
                "ShopLogMessage", "EvalLogMessage", "GradeMessage", "EvalErrorMessage", "RunnerMessage",
                "StubMessage", "CommonGradeMessage", "ConformanceMessage", "ApiErrorMessage"
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
                if (Template(member) is not { } template)
                {
                    problems.Add($"{name} has no [Description]");
                    continue;
                }

                if (Hole().Replace(template, "").IndexOfAny(['{', '}']) >= 0) problems.Add($"{name}: \"{template}\"");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    /// <summary>The text of a member's [Description], or null when it has none.</summary>
    private static string? Template(EnumMemberDeclarationSyntax member)
    {
        // Take the first argument of the Description attribute; its text is the template.
        var argument = member.AttributeLists.SelectMany(list => list.Attributes)
            .Where(attribute => attribute.Name.ToString() is "Description" or "DescriptionAttribute")
            .Select(attribute => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression)
            .FirstOrDefault();
        return argument is null ? null : Text(argument);
    }

    /// <summary>
    /// A string written as one literal, or as literals joined with <c>+</c> to keep a line under 120, with its escapes
    /// undone; nothing for anything else.
    /// </summary>
    /// <remarks>A switch expression on the syntax's shape, one arm per case (*No nested conditionals*).</remarks>
    private static string? Text(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => literal.Token.ValueText,
        BinaryExpressionSyntax sum when sum.IsKind(SyntaxKind.AddExpression)
                                        && Text(sum.Left) is { } left
                                        && Text(sum.Right) is { } right => left + right,
        _ => null,
    };

    /// <summary>A named hole in a template: an opening brace, a name, and a closing brace.</summary>
    [GeneratedRegex(@"\{[A-Za-z][A-Za-z0-9]*\}")]
    private static partial Regex Hole();
}
