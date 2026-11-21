using LouisAgent.Web.Components.Editing.Inline;

namespace LouisAgent.Web.Tests.Components.Editing.Inline;

/// <summary>
/// F142: the small buttons beside an item of a list that is edited on its page: move it a step earlier or later,
/// take it out, and, for an item with more to it than its words, open it. Each button says what it does to which
/// item, for someone who can't see where it sits.
/// </summary>
public class ItemToolsTests
{
    /// <summary>What the tools last asked for.</summary>
    private sealed class Asked
    {
        /// <summary>The steps asked for, in order.</summary>
        public List<int> Moves { get; } = [];

        /// <summary>How many times the item was to be taken out.</summary>
        public int Removes { get; set; }
    }

    /// <summary>The tools of the item at this place in a list of three facts.</summary>
    private static IRenderedComponent<ItemTools> Render(BunitContext ctx, Asked asked, int at) =>
        ctx.Render<ItemTools>(parameters => parameters
            .Add(tools => tools.Noun, "fact")
            .Add(tools => tools.At, at)
            .Add(tools => tools.Count, 3)
            .Add(tools => tools.OnMove, step => asked.Moves.Add(step))
            .Add(tools => tools.OnRemove, () => asked.Removes++));

    /// <summary>Each button says what it does, and to which item.</summary>
    [Test]
    public void EachButton_SaysWhatItDoes_AndToWhich()
    {
        using var ctx = new BunitContext();

        var tools = Render(ctx, new Asked(), at: 1);

        Assert.That(tools.FindAll("button.item-tool").Select(button => button.GetAttribute("aria-label")),
            Is.EqualTo(new[] { "Move fact 2 earlier", "Move fact 2 later", "Remove fact 2" }));
    }

    /// <summary>The buttons ask for a step earlier, a step later, and for the item to go.</summary>
    [Test]
    public void TheButtons_AskForAStep_OrForTheItemToGo()
    {
        using var ctx = new BunitContext();
        var asked = new Asked();
        var tools = Render(ctx, asked, at: 1);
        var buttons = tools.FindAll("button.item-tool");

        buttons[0].Click();
        buttons[1].Click();
        buttons[2].Click();

        Assert.Multiple(() =>
        {
            Assert.That(asked.Moves, Is.EqualTo(new[] { -1, 1 }));
            Assert.That(asked.Removes, Is.EqualTo(1));
        });
    }

    /// <summary>The first item can't move earlier, and the last can't move later.</summary>
    [Test]
    public void TheEnds_HaveNowhereFurtherToGo()
    {
        using var ctx = new BunitContext();

        var first = Render(ctx, new Asked(), at: 0).FindAll("button.item-tool");
        var last = Render(ctx, new Asked(), at: 2).FindAll("button.item-tool");

        Assert.Multiple(() =>
        {
            Assert.That(first[0].HasAttribute("disabled"), Is.True);
            Assert.That(first[1].HasAttribute("disabled"), Is.False);
            Assert.That(last[1].HasAttribute("disabled"), Is.True);
        });
    }

    /// <summary>An item with more to it than its words has a button that opens it, first in the row.</summary>
    [Test]
    public void AnItemThatOpens_HasAButtonForIt()
    {
        using var ctx = new BunitContext();
        int opened = 0;

        var tools = ctx.Render<ItemTools>(parameters => parameters
            .Add(item => item.Noun, "link")
            .Add(item => item.At, 0)
            .Add(item => item.Count, 1)
            .Add(item => item.OnOpen, () => opened++));
        tools.Find("button.item-tool").Click();

        Assert.Multiple(() =>
        {
            Assert.That(tools.Find("button.item-tool").GetAttribute("aria-label"), Is.EqualTo("Change link 1"));
            Assert.That(opened, Is.EqualTo(1));
        });
    }
}
