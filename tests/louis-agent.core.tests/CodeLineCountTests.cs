namespace LouisAgent.Core.Tests;

/// <summary>
/// Pins what the size rule counts: code only, so a summary or a blank line between groups never forces a split
/// (docs/CLAUDE.md, Separate by concern; ADR 0044).
/// </summary>
public class CodeLineCountTests
{
    /// <summary>Blank lines don't count, however much whitespace is on them.</summary>
    [Test]
    public void BlankLines_AreNotCounted()
    {
        string[] lines = ["int a = 1;", "", "   ", "\t", "int b = 2;"];

        Assert.That(FileSizeTests.CountCodeLines(lines), Is.EqualTo(2));
    }

    /// <summary>A line that starts with // or /// is a comment, indented or not.</summary>
    [Test]
    public void CommentLines_AreNotCounted()
    {
        string[] lines =
            ["/// <summary>Does it.</summary>", "    // why", "int a = 1;", "        /// <remarks>x</remarks>"];

        Assert.That(FileSizeTests.CountCodeLines(lines), Is.EqualTo(1));
    }

    /// <summary>A comment after code is still code: the line has code on it.</summary>
    [Test]
    public void ACodeLineWithATrailingComment_IsCounted()
    {
        string[] lines = ["int a = 1; // one", "var url = \"http://x\";"];

        Assert.That(FileSizeTests.CountCodeLines(lines), Is.EqualTo(2));
    }
}
