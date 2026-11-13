using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Editing.Inline;

/// <summary>
/// F142: an item with more to it than its words (a link has a network, a name and an address) is changed in a small
/// form on its page, the same way as words are: the edits are a draft of the item, the tick keeps them all and the
/// cross drops them all. Nothing reaches the owner until the tick.
/// </summary>
public class InlineFormTests
{
    /// <summary>A link, as a sample of an item with several fields.</summary>
    private sealed record Link(string Network, string Address);

    /// <summary>What the form handed back.</summary>
    private sealed class Ends
    {
        /// <summary>The item as kept; nothing until the tick.</summary>
        public Link? Kept { get; set; }

        /// <summary>How many times the edits were dropped.</summary>
        public int Drops { get; set; }
    }

    /// <summary>The form over a link, with a box for its address.</summary>
    private static IRenderedComponent<InlineForm<Link>> Render(BunitContext ctx, Ends ends) =>
        ctx.Render<InlineForm<Link>>(parameters => parameters
            .Add(form => form.Item, new Link("GitHub", "https://github.com/old"))
            .Add(form => form.Label, "link 1")
            .Add(form => form.OnKeep, kept => ends.Kept = kept)
            .Add(form => form.OnDrop, () => ends.Drops++)
            .Add(form => form.ChildContent, row => builder =>
            {
                builder.OpenComponent<TextField>(0);
                builder.AddComponentParameter(1, nameof(TextField.Label), "Address");
                builder.AddComponentParameter(2, nameof(TextField.Value), row.Item.Address);
                builder.AddComponentParameter(3, nameof(TextField.ValueChanged),
                    EventCallback.Factory.Create<string?>(row, address =>
                        row.Changed.InvokeAsync(row.Item with { Address = address ?? "" })));
                builder.CloseComponent();
            }));

    /// <summary>The form is a named group holding the item's fields, with a tick and a cross.</summary>
    [Test]
    public void TheForm_HoldsTheItemsFields_WithATickAndACross()
    {
        using var ctx = new BunitContext();

        var form = Render(ctx, new Ends());

        Assert.Multiple(() =>
        {
            Assert.That(form.Find(".inline-form").GetAttribute("role"), Is.EqualTo("group"));
            Assert.That(form.Find(".inline-form").GetAttribute("aria-label"), Is.EqualTo("Change link 1"));
            Assert.That(form.Find(".field-box").GetAttribute("value"), Is.EqualTo("https://github.com/old"));
            Assert.That(form.FindAll("button.inline-keep"), Has.Count.EqualTo(1));
            Assert.That(form.FindAll("button.inline-drop"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>An edit is a draft: the box shows it, and the owner hears nothing until the tick.</summary>
    [Test]
    public void TheTick_KeepsEveryEdit_AtOnce()
    {
        using var ctx = new BunitContext();
        var ends = new Ends();
        var form = Render(ctx, ends);

        form.Find(".field-box").Input("https://github.com/new");
        var beforeTheTick = ends.Kept;
        form.Find("button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(beforeTheTick, Is.Null);
            Assert.That(form.Find(".field-box").GetAttribute("value"), Is.EqualTo("https://github.com/new"));
            Assert.That(ends.Kept, Is.EqualTo(new Link("GitHub", "https://github.com/new")));
        });
    }

    /// <summary>The cross drops the edits: the owner is told to close, and is handed nothing.</summary>
    [Test]
    public void TheCross_DropsEveryEdit()
    {
        using var ctx = new BunitContext();
        var ends = new Ends();
        var form = Render(ctx, ends);

        form.Find(".field-box").Input("https://github.com/new");
        form.Find("button.inline-drop").Click();

        Assert.Multiple(() =>
        {
            Assert.That(ends.Kept, Is.Null);
            Assert.That(ends.Drops, Is.EqualTo(1));
        });
    }
}
