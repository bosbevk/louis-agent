using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// Finds the strings in a C# source that spell one of our wire names, so a name that repeats is an enum member and not
/// text. It reads the code as C#, so where the formatter breaks a line makes no difference. A name inside an enum, or in a
/// class asked for by name, is where it is written once, and a string that is really a program or a key is let through
/// (<see cref="NotAToolset"/>).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class NameScan
{
    /// <summary>A string that spells a name: what it says, the line it is on, and that line's text.</summary>
    public sealed record Hit(int Line, string Value, string Text);

    /// <summary>The plain string literals of the source that spell one of the names and are not excused, with their lines.</summary>
    public static IReadOnlyList<Hit> Flagged(string text, ISet<string> names,
        IReadOnlyCollection<string>? skippedClasses = null)
    {
        var tree = CSharpSyntaxTree.ParseText(text);
        var lines = tree.GetText().Lines;

        // Only plain strings count: a name inside a raw, verbatim or interpolated string is wire data (an expected JSON, a message).
        return
        [
            .. tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(literal => IsPlainString(literal) && names.Contains(literal.Token.ValueText))
                .Where(literal => !InsideAnInterpolatedString(literal) &&
                                  !InsideASkippedBody(literal, skippedClasses ?? []) && !NotAToolset.Excuses(literal))
                .Select(literal =>
                {
                    int line = tree.GetLineSpan(literal.Span).StartLinePosition.Line;
                    return new Hit(line + 1, literal.Token.ValueText, lines[line].ToString().Trim());
                })
        ];
    }

    /// <summary>A string written with plain quotes: not a raw one (three quotes or more), and not a verbatim one.</summary>
    private static bool IsPlainString(LiteralExpressionSyntax literal) =>
        literal.IsKind(SyntaxKind.StringLiteralExpression) && literal.Token.Text.StartsWith('"') &&
        !literal.Token.Text.StartsWith("\"\"\"", StringComparison.Ordinal);

    /// <summary>A string typed inside the braces of an interpolated one, which is text with a hole in it.</summary>
    private static bool InsideAnInterpolatedString(SyntaxNode literal) =>
        literal.Ancestors().Any(ancestor => ancestor is InterpolatedStringExpressionSyntax);

    /// <summary>Inside the braces of an enum, or of a class asked for by name: where a name is written once.</summary>
    private static bool InsideASkippedBody(SyntaxNode literal, IReadOnlyCollection<string> skippedClasses) =>
        literal.Ancestors().Any(ancestor => ancestor switch
        {
            EnumDeclarationSyntax declaration =>
                InBraces(literal, declaration.OpenBraceToken, declaration.CloseBraceToken),
            ClassDeclarationSyntax declaration when skippedClasses.Contains(declaration.Identifier.Text) =>
                InBraces(literal, declaration.OpenBraceToken, declaration.CloseBraceToken),
            _ => false,
        });

    /// <summary>Whether the string sits between the two braces.</summary>
    private static bool InBraces(SyntaxNode literal, SyntaxToken open, SyntaxToken close) =>
        literal.SpanStart > open.SpanStart && literal.Span.End <= close.SpanStart;
}
