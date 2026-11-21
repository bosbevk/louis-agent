using LouisAgent.Core.Agent.Turns;

namespace LouisAgent.Core.Tests.Agent.Turns;

// S (CL15): a tool call's arguments on one line for a progress display (the CLI's status line, Rider's tool card).
public class ToolCallTextTests
{
    [Test]
    public void NoArguments_IsNothing()
    {
        Assert.That(ToolCallText.Describe(null), Is.Empty);
        Assert.That(ToolCallText.Describe(new Dictionary<string, object?>()), Is.Empty);
    }

    [Test]
    public void EachArgument_AsKeyEqualsValue_Joined() =>
        Assert.That(ToolCallText.Describe(new Dictionary<string, object?> { ["path"] = "src/A.cs", ["lines"] = 3, ["flag"] = null }),
            Is.EqualTo("path=src/A.cs, lines=3, flag=null"));

    [Test]
    public void ALongValue_IsCut_AndLineBreaksFlattened()
    {
        string described = ToolCallText.Describe(new Dictionary<string, object?> { ["content"] = "line one\nline two " + new string('x', 80) });

        Assert.That(described, Does.StartWith("content=line one line two "));
        Assert.That(described, Does.EndWith("..."));
        Assert.That(described, Has.Length.EqualTo("content=".Length + 60 + 3));
    }
}
