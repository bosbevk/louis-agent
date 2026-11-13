using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Tests.Components.Editing.Inline;

/// <summary>
/// F142, Louis: "can we not just edit it inline?", and his pick of three for how: click the words, a box takes their
/// place with a tick and a cross, Enter keeps and Escape drops. A piece of text is editable exactly when something
/// listens for its change, so the same component draws a public page's plain words.
/// </summary>
public class InlineEditTests
{
    /// <summary>What the text last handed back, and how many times.</summary>
    private sealed class Kept
    {
        /// <summary>The words as last kept.</summary>
        public string? Value { get; set; }

        /// <summary>How many times words were kept.</summary>
        public int Times { get; set; }
    }

    /// <summary>The text with something listening for its change.</summary>
    private static IRenderedComponent<InlineEdit> Editable(BunitContext ctx, Kept kept, string? value,
        bool multiline = false)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<InlineEdit>(parameters => parameters
            .Add(text => text.Label, "Name")
            .Add(text => text.Value, value)
            .Add(text => text.Multiline, multiline)
            .Add(text => text.ValueChanged, changed =>
            {
                kept.Value = changed;
                kept.Times++;
            }));
    }

    /// <summary>With nothing listening it is the words and nothing else, so a public page is drawn as before.</summary>
    [Test]
    public void WithNothingListening_ItIsThePlainWords()
    {
        using var ctx = new BunitContext();

        var text = ctx.Render<InlineEdit>(parameters => parameters
            .Add(words => words.Label, "Name")
            .Add(words => words.Value, "Louis Marais"));

        Assert.That(text.Markup, Is.EqualTo("Louis Marais"));
    }

    /// <summary>With something listening, the words can be clicked, or reached and opened from the keyboard.</summary>
    [Test]
    public void WithSomethingListening_TheWordsOpenForEditing()
    {
        using var ctx = new BunitContext();
        var text = Editable(ctx, new Kept(), "Louis Marais");
        var words = text.Find(".inline-read");

        Assert.Multiple(() =>
        {
            Assert.That(words.TextContent, Is.EqualTo("Louis Marais"));
            Assert.That(words.GetAttribute("role"), Is.EqualTo("button"));
            Assert.That(words.GetAttribute("tabindex"), Is.EqualTo("0"));
            Assert.That(text.FindAll(".inline-box"), Is.Empty, "no box until it is asked for");
        });
    }

    /// <summary>A click swaps the words for a box that holds them, with a tick and a cross.</summary>
    [Test]
    public void AClick_SwapsTheWordsForABox_WithATickAndACross()
    {
        using var ctx = new BunitContext();
        var text = Editable(ctx, new Kept(), "Louis Marais");

        text.Find(".inline-read").Click();

        Assert.Multiple(() =>
        {
            Assert.That(text.Find("input.inline-box").GetAttribute("value"), Is.EqualTo("Louis Marais"));
            Assert.That(text.Find("input.inline-box").GetAttribute("aria-label"), Is.EqualTo("Name"));
            Assert.That(text.FindAll("button.inline-keep"), Has.Count.EqualTo(1));
            Assert.That(text.FindAll("button.inline-drop"), Has.Count.EqualTo(1));
            Assert.That(text.FindAll(".inline-read"), Is.Empty);
        });
    }

    /// <summary>Enter keeps what was typed, tidied, and the words are back.</summary>
    [Test]
    public void Enter_KeepsWhatWasTyped()
    {
        using var ctx = new BunitContext();
        var kept = new Kept();
        var text = Editable(ctx, kept, "Louis Marais");
        text.Find(".inline-read").Click();
        text.Find("input.inline-box").Input("  Louis J Marais ");

        text.Find("input.inline-box").KeyDown(Key.Enter);

        Assert.Multiple(() =>
        {
            Assert.That(kept.Value, Is.EqualTo("Louis J Marais"));
            Assert.That(text.FindAll(".inline-box"), Is.Empty);
            Assert.That(text.FindAll(".inline-read"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>Escape drops what was typed: nothing is handed back, and the words are as they were.</summary>
    [Test]
    public void Escape_DropsWhatWasTyped()
    {
        using var ctx = new BunitContext();
        var kept = new Kept();
        var text = Editable(ctx, kept, "Louis Marais");
        text.Find(".inline-read").Click();
        text.Find("input.inline-box").Input("Someone else");

        text.Find("input.inline-box").KeyDown(Key.Escape);

        Assert.Multiple(() =>
        {
            Assert.That(kept.Times, Is.Zero);
            Assert.That(text.Find(".inline-read").TextContent, Is.EqualTo("Louis Marais"));
        });
    }

    /// <summary>The tick keeps and the cross drops, for a hand on the mouse.</summary>
    [Test]
    public void TheTickKeeps_AndTheCrossDrops()
    {
        using var ctx = new BunitContext();
        var kept = new Kept();
        var text = Editable(ctx, kept, "Louis Marais");

        text.Find(".inline-read").Click();
        text.Find("input.inline-box").Input("Dropped");
        text.Find("button.inline-drop").Click();
        text.Find(".inline-read").Click();
        text.Find("input.inline-box").Input("Kept");
        text.Find("button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(kept.Value, Is.EqualTo("Kept"));
            Assert.That(kept.Times, Is.EqualTo(1));
        });
    }

    /// <summary>Words kept as they were are no change, so nothing is handed back.</summary>
    [Test]
    public void WordsKeptAsTheyWere_AreNoChange()
    {
        using var ctx = new BunitContext();
        var kept = new Kept();
        var text = Editable(ctx, kept, "Louis Marais");
        text.Find(".inline-read").Click();

        text.Find("input.inline-box").KeyDown(Key.Enter);

        Assert.That(kept.Times, Is.Zero);
    }

    /// <summary>
    /// A longer text is a box of several lines, where Enter is a new line and Ctrl with Enter keeps.
    /// </summary>
    [Test]
    public void ALongerText_TakesNewLines_AndKeepsOnCtrlEnter()
    {
        using var ctx = new BunitContext();
        var kept = new Kept();
        var text = Editable(ctx, kept, "One line.", multiline: true);
        text.Find(".inline-read").Click();
        text.Find("textarea.inline-box").Input("Two lines.");

        text.Find("textarea.inline-box").KeyDown(Key.Enter);
        int afterEnter = kept.Times;
        var ctrlEnter = new KeyboardEventArgs
        {
            Key = "Enter",
            CtrlKey = true
        };
        text.Find("textarea.inline-box").KeyDown(ctrlEnter);

        Assert.Multiple(() =>
        {
            Assert.That(afterEnter, Is.Zero, "Enter alone is a new line");
            Assert.That(kept.Value, Is.EqualTo("Two lines."));
        });
    }

    /// <summary>
    /// A box emptied and kept hands back nothing at all, and where there are no words a quiet stand-in says what
    /// goes there, so it can still be clicked.
    /// </summary>
    [Test]
    public void NoWords_HandBackNothing_AndLeaveAStandIn()
    {
        using var ctx = new BunitContext();
        var kept = new Kept { Value = "unset" };
        var text = Editable(ctx, kept, "Louis Marais");
        text.Find(".inline-read").Click();
        text.Find("input.inline-box").Input("   ");
        text.Find("input.inline-box").KeyDown(Key.Enter);

        var empty = Editable(ctx, new Kept(), value: null);

        Assert.Multiple(() =>
        {
            Assert.That(kept.Value, Is.Null);
            Assert.That(empty.Find(".inline-read.is-empty").TextContent, Is.EqualTo("Name"));
        });
    }
}
