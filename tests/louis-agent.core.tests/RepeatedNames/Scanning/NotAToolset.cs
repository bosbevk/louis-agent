using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// Says when a string that has the name of a toolset or a language is really something else: the program a process
/// starts, a key read from JSON, a role's folder. Each way is one small rule about where the string sits in the code.
/// </summary>
/// <remarks>
/// Specification. Every way a string is excused is its own method that answers yes or no, and <see cref="Excuses"/>
/// only asks them in turn. Reading the text before the string did the same job with a dozen patterns in two regexes,
/// and the formatter broke them by moving the text to another line. Without it a new way to excuse a string would be
/// another pattern in a regex, not another rule with a name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class NotAToolset
{
    /// <summary>What a call is named when its first argument is a program or a key, not a toolset.</summary>
    private static readonly string[] CallsTakingAProgramOrAKey =
    [
        "ProcessStartInfo", "Process.Start", "ProcessRunner.Run", "GetProperty", "TryGetProperty",
        "Has.None.StartsWith",
        "Has.None.Contains", "Does.Not.Contain"
    ];

    /// <summary>The names a string is the value of when it is a program or a role level's folder.</summary>
    private static readonly string[] TargetsOfAProgramOrFolder =
        ["evalHost", "DotNetHost", "SpecialistsFolder", "MinionsFolder"];

    /// <summary>The names that, when they come first in a statement, make the strings after them programs.</summary>
    private static readonly string[] NamesOfProgramLists =
        ["candidates", "CommonCommands", "ResolveShell", "_venvDirectory"];

    /// <summary>Whether this string is one of the things that are not a toolset or a language.</summary>
    public static bool Excuses(LiteralExpressionSyntax literal) =>
        IsTheFirstArgumentOfAProgramOrKeyCall(literal) || IsAnIndex(literal) || IsTheValueOfAProgramOrFolder(literal) ||
        FollowsAProgramNameInItsStatement(literal);

    /// <summary>It is what a call is given first: <c>Process.Start("git")</c>, <c>element.GetProperty("web")</c>.</summary>
    private static bool IsTheFirstArgumentOfAProgramOrKeyCall(LiteralExpressionSyntax literal)
    {
        if (literal.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: { } call } list } argument ||
            list.Arguments[0] != argument) return false;

        // The callee without its spaces and line breaks, so it reads the same wherever the formatter breaks the line.
        string callee = call switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression.ToString(),
            ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
            _ => "",
        };
        callee = string.Concat(callee.Where(c => !char.IsWhiteSpace(c)));
        return CallsTakingAProgramOrAKey.Any(name =>
            callee == name || callee.EndsWith($".{name}", StringComparison.Ordinal));
    }

    /// <summary>It is a key in square brackets: <c>settings["web"]</c>.</summary>
    private static bool IsAnIndex(LiteralExpressionSyntax literal) =>
        literal.Parent is ArgumentSyntax
        {
            Parent: BracketedArgumentListSyntax { Parent: ElementAccessExpressionSyntax }
        };

    /// <summary>It is what a program's or a folder's name is set to, or what the <c>EVALS</c> key is set to.</summary>
    private static bool IsTheValueOfAProgramOrFolder(LiteralExpressionSyntax literal) => literal.Parent switch
    {
        EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } =>
            TargetsOfAProgramOrFolder.Contains(declarator.Identifier.Text),
        EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property } =>
            TargetsOfAProgramOrFolder.Contains(property.Identifier.Text),
        EqualsValueClauseSyntax { Parent: ParameterSyntax parameter } =>
            TargetsOfAProgramOrFolder.Contains(parameter.Identifier.Text),
        AssignmentExpressionSyntax assignment when assignment.Right == literal =>
            TargetsOfAProgramOrFolder.Contains(NameOf(assignment.Left)) || IsTheEvalsKey(assignment.Left),
        _ => false,
    };

    /// <summary>It comes after something in its own statement that makes it a program: a list of commands, a file name, the wire names of an enum.</summary>
    private static bool FollowsAProgramNameInItsStatement(LiteralExpressionSyntax literal)
    {
        // The statement, or the field or property, that holds the string: what is said before it in there counts, wherever the line breaks.
        var statement = literal.Ancestors().FirstOrDefault(node => node is StatementSyntax or MemberDeclarationSyntax);
        if (statement is null) return false;

        var before = statement.DescendantNodesAndTokens().Where(item => item.Span.End <= literal.SpanStart).ToList();
        return before.Any(item => item.IsToken && NamesOfProgramLists.Contains(item.AsToken().Text)) ||
               before.Any(item => item.AsNode() is MemberAccessExpressionSyntax { Name.Identifier.Text: "FileName" }) ||
               before.Any(item => IsTheWireNamesOfAnEnum(item.AsNode()));
    }

    /// <summary>It is the <c>Enum.GetValues&lt;T&gt;().Select(x =&gt; x.Wire())</c> a test compares with the names it lists.</summary>
    private static bool IsTheWireNamesOfAnEnum(SyntaxNode? node) =>
        node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "Select",
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Expression: IdentifierNameSyntax { Identifier.Text: "Enum" },
                        Name: GenericNameSyntax { Identifier.Text: "GetValues" }
                    }
                }
            }
        } select && select.ArgumentList.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Wire" });

    /// <summary>The key <c>["EVALS"]</c> on the left of an assignment.</summary>
    private static bool IsTheEvalsKey(ExpressionSyntax left) => left switch
    {
        ElementAccessExpressionSyntax access => FirstKeyIs(access.ArgumentList, "EVALS"),
        ImplicitElementAccessSyntax access => FirstKeyIs(access.ArgumentList, "EVALS"),
        _ => false,
    };

    /// <summary>Whether the first key in the brackets is this text.</summary>
    private static bool FirstKeyIs(BracketedArgumentListSyntax keys, string text) =>
        keys.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax { Token.ValueText: var key } &&
        key == text;

    /// <summary>The name an assignment sets: <c>host</c> in <c>host = ...</c> and in <c>options.host = ...</c>.</summary>
    private static string NameOf(ExpressionSyntax left) => left switch
    {
        IdentifierNameSyntax name => name.Identifier.Text,
        MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
        _ => "",
    };
}
