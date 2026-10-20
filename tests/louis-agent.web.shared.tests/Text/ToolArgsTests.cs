using LouisAgent.Web.Shared.Text;

namespace LouisAgent.Web.Shared.Tests.Text;

// W2b: a tool call's input on its one summary line: the JSON flattened, its braces gone, cut at eighty characters.
public class ToolArgsTests
{
    [Test]
    public void TheInput_IsOneLine_WithoutItsBraces() =>
        Assert.That(ToolArgs.Summarise("{\n  \"path\": \"README.md\",\n  \"lines\": 5\n}"), Is.EqualTo("\"path\": \"README.md\", \"lines\": 5"));

    [Test]
    public void ALongInput_IsCut_WithAnEllipsis()
    {
        string summary = ToolArgs.Summarise("{\"text\": \"" + new string('a', 100) + "\"}");

        Assert.That(summary, Has.Length.EqualTo(81).And.EndWith("…"));
        Assert.That(summary, Does.StartWith("\"text\": \"aaa"));
    }

    [Test]
    public void AShortInput_IsKept() =>
        Assert.That(ToolArgs.Summarise("{\"q\": \"wat is nuut\"}"), Is.EqualTo("\"q\": \"wat is nuut\""));
}
