using LouisAgent.Web.Components.Editing.Inline;

namespace LouisAgent.Web.Tests.Components.Editing.Inline;

/// <summary>
/// F142: under a list that is edited on its page, a quiet button that asks for one more item, in the list's own
/// words: Add a fact, Add a link.
/// </summary>
public class ItemAddTests
{
    /// <summary>The button says what it adds, and asks for it when clicked.</summary>
    [Test]
    public void TheButton_SaysWhatItAdds_AndAsksForIt()
    {
        using var ctx = new BunitContext();
        int asked = 0;

        var add = ctx.Render<ItemAdd>(parameters => parameters
            .Add(button => button.Label, "Add a fact")
            .Add(button => button.OnAdd, () => asked++));
        add.Find("button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(add.Find("button.item-add").TextContent.Trim(), Is.EqualTo("+ Add a fact"));
            Assert.That(add.Find("button.item-add").GetAttribute("type"), Is.EqualTo("button"));
            Assert.That(asked, Is.EqualTo(1));
        });
    }
}
