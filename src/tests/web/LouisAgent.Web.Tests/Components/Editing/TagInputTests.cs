using LouisAgent.Web.Components.Editing;

namespace LouisAgent.Web.Tests.Components.Editing;

/// <summary>
/// F142: a field of short words (a job's stack, a skill group's skills): the tags sit in the field as chips, typing
/// after them and pressing Enter or a comma adds one, a chip's cross removes it. Each change hands back a new list.
/// </summary>
public class TagInputTests
{
    /// <summary>What the field last handed back.</summary>
    private sealed class Holder
    {
        /// <summary>The tags as last changed, or none while nothing changed.</summary>
        public IReadOnlyList<string>? Tags { get; set; }
    }

    /// <summary>The field over these tags.</summary>
    private static IRenderedComponent<TagInput> Render(BunitContext ctx, Holder holder, params string[] tags) =>
        ctx.Render<TagInput>(parameters => parameters
            .Add(field => field.Tags, tags)
            .Add(field => field.TagsChanged, changed => holder.Tags = changed)
            .Add(field => field.Label, "Stack"));

    /// <summary>The tags are chips in the field, in order, and the box after them carries the field's name.</summary>
    [Test]
    public void TheTags_AreChipsInTheField()
    {
        using var ctx = new BunitContext();

        var field = Render(ctx, new Holder(), "C#", "Azure");

        Assert.Multiple(() =>
        {
            Assert.That(field.FindAll(".tag-word").Select(tag => tag.TextContent), Is.EqualTo(new[] { "C#", "Azure" }));
            Assert.That(field.Find("input.tag-entry").GetAttribute("aria-label"), Is.EqualTo("Stack"));
        });
    }

    /// <summary>Typing a word and pressing Enter adds it, trimmed, and empties the box.</summary>
    [Test]
    public void Enter_AddsTheWord_AndEmptiesTheBox()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "C#");

        field.Find("input.tag-entry").Input("  Azure ");
        field.Find("input.tag-entry").KeyDown("Enter");

        Assert.Multiple(() =>
        {
            Assert.That(holder.Tags, Is.EqualTo(new[] { "C#", "Azure" }));
            Assert.That(field.Find("input.tag-entry").GetAttribute("value"), Is.Empty);
        });
    }

    /// <summary>A comma adds what was typed before it, so a pasted list becomes tags.</summary>
    [Test]
    public void AComma_AddsWhatCameBeforeIt()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder);

        field.Find("input.tag-entry").Input("Docker, Bicep,");

        Assert.That(holder.Tags, Is.EqualTo(new[] { "Docker", "Bicep" }));
    }

    /// <summary>An empty word, or one already there (in any case), adds nothing.</summary>
    [TestCase("   ")]
    [TestCase("azure")]
    public void AnEmptyOrRepeatedWord_AddsNothing(string typed)
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "Azure");

        field.Find("input.tag-entry").Input(typed);
        field.Find("input.tag-entry").KeyDown("Enter");

        Assert.That(holder.Tags, Is.Null);
    }

    /// <summary>A chip's cross removes that tag.</summary>
    [Test]
    public void AChipsCross_RemovesTheTag()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "C#", "Azure");

        field.Find("button[aria-label='Remove C#']").Click();

        Assert.That(holder.Tags, Is.EqualTo(new[] { "Azure" }));
    }

    /// <summary>Backspace in an empty box takes the last tag off, as in any tag field.</summary>
    [Test]
    public void BackspaceInAnEmptyBox_RemovesTheLastTag()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "C#", "Azure");

        field.Find("input.tag-entry").KeyDown("Backspace");

        Assert.That(holder.Tags, Is.EqualTo(new[] { "C#" }));
    }

    /// <summary>Leaving the box keeps what was typed, so a word isn't lost for want of an Enter.</summary>
    [Test]
    public void LeavingTheBox_KeepsWhatWasTyped()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "C#");

        field.Find("input.tag-entry").Input("Azure");
        field.Find("input.tag-entry").Blur();

        Assert.That(holder.Tags, Is.EqualTo(new[] { "C#", "Azure" }));
    }
}
