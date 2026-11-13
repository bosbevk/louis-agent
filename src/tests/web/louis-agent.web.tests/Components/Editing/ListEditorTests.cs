using LouisAgent.Web.Components.Editing;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Editing;

/// <summary>
/// F142: a list a person edits in a form (jobs, schools, skill groups): a line per item that folds open on its
/// fields, with add, remove and reorder. The list is never changed in place: each action hands back a new list, so
/// the page that holds it stays the one owner of what is being edited.
/// </summary>
public class ListEditorTests
{
    /// <summary>What the editor last handed back.</summary>
    private sealed class Holder
    {
        /// <summary>The list as last changed, or none while nothing changed.</summary>
        public IReadOnlyList<string>? Items { get; set; }
    }

    /// <summary>The editor over these words, each row's field an input a test can type in.</summary>
    private static IRenderedComponent<ListEditor<string>> Render(BunitContext ctx, Holder holder,
        params string[] items) =>
        ctx.Render<ListEditor<string>>(parameters => parameters
            .Add(editor => editor.Items, items)
            .Add(editor => editor.ItemsChanged, changed => holder.Items = changed)
            .Add(editor => editor.New, () => "")
            .Add(editor => editor.TitleOf, item => item)
            .Add(editor => editor.Noun, "word")
            .Add(editor => editor.AddLabel, "Add a word")
            .Add(editor => editor.ChildContent, row => builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "class", "row-field");
                builder.AddAttribute(2, "value", row.Item);
                builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(row,
                    e => row.Changed.InvokeAsync((string)e.Value!)));
                builder.CloseElement();
            }));

    /// <summary>A line per item, in order, each with its place and its title; all folded.</summary>
    [Test]
    public void TheList_IsALinePerItem_InOrder_Folded()
    {
        using var ctx = new BunitContext();

        var editor = Render(ctx, new Holder(), "first", "second");

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindAll(".list-row-title").Select(title => title.TextContent),
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(editor.FindAll(".list-row-place").Select(place => place.TextContent),
                Is.EqualTo(new[] { "1", "2" }));
            Assert.That(editor.FindAll(".list-row-body"), Is.Empty);
        });
    }

    /// <summary>A click on a line opens its fields, and a second click folds it again.</summary>
    [Test]
    public void ALine_OpensOnItsFields_AndFoldsAgain()
    {
        using var ctx = new BunitContext();
        var editor = Render(ctx, new Holder(), "first", "second");

        editor.FindAll(".list-row-toggle")[1].Click();
        var open = editor.FindAll(".list-row-body .row-field").Select(field => field.GetAttribute("value")).ToList();
        string? expanded = editor.FindAll(".list-row-toggle")[1].GetAttribute("aria-expanded");
        editor.FindAll(".list-row-toggle")[1].Click();

        Assert.Multiple(() =>
        {
            Assert.That(open, Is.EqualTo(new[] { "second" }));
            Assert.That(expanded, Is.EqualTo("true"));
            Assert.That(editor.FindAll(".list-row-body"), Is.Empty);
        });
    }

    /// <summary>Add hands back the list with a new item at its end, and opens it to be filled in.</summary>
    [Test]
    public void Add_AppendsANewItem_AndOpensIt()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var editor = Render(ctx, holder, "first");

        editor.Find(".list-add").Click();
        editor.Render(parameters => parameters.Add(e => e.Items, holder.Items!));

        Assert.Multiple(() =>
        {
            Assert.That(holder.Items, Is.EqualTo(new[] { "first", "" }));
            Assert.That(editor.Find(".list-add").TextContent, Does.Contain("Add a word"));
            Assert.That(editor.FindAll(".list-row-body"), Has.Count.EqualTo(1), "the new one is open");
            Assert.That(editor.FindAll(".list-row-title")[1].TextContent, Is.EqualTo("New word"));
        });
    }

    /// <summary>Remove hands back the list without that item.</summary>
    [Test]
    public void Remove_DropsTheItem()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var editor = Render(ctx, holder, "first", "second", "third");

        editor.Find("button[aria-label='Remove word 2']").Click();

        Assert.That(holder.Items, Is.EqualTo(new[] { "first", "third" }));
    }

    /// <summary>Move up and move down swap an item with its neighbour; the ends can't move past the list.</summary>
    [Test]
    public void Move_SwapsWithItsNeighbour_AndTheEndsStay()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var editor = Render(ctx, holder, "first", "second", "third");

        editor.Find("button[aria-label='Move word 3 up']").Click();
        var up = holder.Items;
        editor.Find("button[aria-label='Move word 1 down']").Click();
        var down = holder.Items;

        Assert.Multiple(() =>
        {
            Assert.That(up, Is.EqualTo(new[] { "first", "third", "second" }));
            Assert.That(down, Is.EqualTo(new[] { "second", "first", "third" }));
            Assert.That(editor.Find("button[aria-label='Move word 1 up']").HasAttribute("disabled"), Is.True);
            Assert.That(editor.Find("button[aria-label='Move word 3 down']").HasAttribute("disabled"), Is.True);
        });
    }

    /// <summary>A row's own change hands back the list with that item replaced, the rest untouched.</summary>
    [Test]
    public void ARowsChange_ReplacesThatItem()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var editor = Render(ctx, holder, "first", "second");
        editor.FindAll(".list-row-toggle")[0].Click();

        editor.Find(".row-field").Change("changed");

        Assert.That(holder.Items, Is.EqualTo(new[] { "changed", "second" }));
    }
}
