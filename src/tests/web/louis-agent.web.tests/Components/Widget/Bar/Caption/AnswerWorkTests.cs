using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar.Caption;
using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption;

/// <summary>
/// F134.5: what an answer did, as the back of its caption shows it: the tools it called, how long it thought, and what
/// the framework measured of it.
/// </summary>
public class AnswerWorkTests
{
    /// <summary>
    /// A turn that thought twice, showed a place, offered choices, called one more tool, and answered.
    /// </summary>
    private static Turn Answered() => new()
    {
        UserText = "Show me a demo",
        Blocks =
        [
            new Block { Kind = BlockKind.Thinking, Text = "…", ThinkingSeconds = 0.75 },
            new Block
            {
                Kind = BlockKind.Tool,
                ToolName = "ShowPage",
                ToolUseId = "toolu_show",
                ToolInput = """{"path":"/restaurant-demo"}""",
                ToolResult = "Shown."
            },
            new Block { Kind = BlockKind.Thinking, Text = "…", ThinkingSeconds = 0.5 },
            new Block
            {
                Kind = BlockKind.Tool,
                ToolName = "ReadDoc",
                ToolUseId = "toolu_read",
                ToolInput = """{"path":"docs/demos.md"}""",
                ToolResult = "…"
            },
            new Block { Kind = BlockKind.Text, Text = "Here it is." },
            new Block
            {
                Kind = BlockKind.Tool,
                ToolName = "Offer",
                ToolUseId = "toolu_offer",
                ToolInput = """{"question":"Which one?","choices":[{"label":"A"},{"label":"B"}]}""",
                ToolResult = "Offered."
            }
        ],
        Usage = new TurnUsage(6, 1400, 37000, 1200, 4, 0.0042m, "USD", 0)
    };

    /// <summary>
    /// Each tool is named in the order it was called, a shown place by its address and an offer by its question; any
    /// other tool by its name alone.
    /// </summary>
    [Test]
    public void TheTools_AreNamed_InTheirOrder()
    {
        var work = AnswerWork.Of(Answered());

        Assert.That(work.Tools, Is.EqualTo(new[]
        {
            new AnswerTool("ShowPage", "/restaurant-demo"),
            new AnswerTool("ReadDoc", null),
            new AnswerTool("Offer", "Which one?")
        }));
    }

    /// <summary>The time it thought is its thinking blocks' together, and its usage is the turn's own.</summary>
    [Test]
    public void TheTimeAndTheUsage_AreTheTurns()
    {
        var turn = Answered();

        var work = AnswerWork.Of(turn);

        Assert.That((work.Thought, work.Usage), Is.EqualTo((1.25, turn.Usage)));
    }

    /// <summary>An answer that called nothing and thought nothing says so: no tools, no time.</summary>
    [Test]
    public void AnAnswerOfWordsAlone_CalledNothing()
    {
        var turn = new Turn { UserText = "Hi", Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] };

        var work = AnswerWork.Of(turn);

        Assert.That((work.Tools.Count, work.Thought, work.Usage), Is.EqualTo((0, 0.0, (TurnUsage?)null)));
    }
}
