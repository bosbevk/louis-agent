using LouisAgent.Web.Text;

namespace LouisAgent.Web.Tests.Text;

/// <summary>A tool call's input on one line: the JSON flattened, its braces gone, cut at eighty characters.</summary>
public class ToolArgsTests
{
    /// <summary>The input is one line, without its braces.</summary>
    [Test]
    public void TheInput_IsOneLine_WithoutItsBraces() =>
        Assert.That(ToolArgs.Summarise("{\n  \"path\": \"README.md\",\n  \"lines\": 5\n}"), Is.EqualTo("\"path\": \"README.md\", \"lines\": 5"));

    /// <summary>A long input is cut, with an ellipsis.</summary>
    [Test]
    public void ALongInput_IsCut_WithAnEllipsis()
    {
        string summary = ToolArgs.Summarise("{\"text\": \"" + new string('a', 100) + "\"}");

        Assert.That(summary, Has.Length.EqualTo(81).And.EndWith("…"));
        Assert.That(summary, Does.StartWith("\"text\": \"aaa"));
    }

    /// <summary>A short input is kept.</summary>
    [Test]
    public void AShortInput_IsKept() =>
        Assert.That(ToolArgs.Summarise("{\"q\": \"wat is nuut\"}"), Is.EqualTo("\"q\": \"wat is nuut\""));
}
