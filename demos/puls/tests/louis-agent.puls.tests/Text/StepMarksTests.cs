using LouisAgent.Puls.Text;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Puls.Tests.Text;

/// <summary>A step in the work panel is waiting until its result arrives, then failed or done.</summary>
public class StepMarksTests
{
    /// <summary>Each of the three states gets its own class name and its own sign.</summary>
    [TestCase(null, false, "waiting", "⋯")]
    [TestCase(null, true, "waiting", "⋯")]
    [TestCase("Error: not found", true, "failed", "✗")]
    [TestCase("R35.99", false, "ok", "✓")]
    public void AStep_IsMarkedByItsResult(string? result, bool failed, string state, string mark)
    {
        var step = new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "FetchUrl",
            ToolResult = result,
            ToolFailed = failed
        };

        Assert.That((StepMarks.StateOf(step), StepMarks.MarkOf(step)), Is.EqualTo((state, mark)));
    }
}
