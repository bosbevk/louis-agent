using LouisAgent.Web.Components.Editing;

namespace LouisAgent.Web.Tests.Components.Editing;

/// <summary>
/// F142: one labelled box of a form a person fills in: its label over it, a line or a box that grows, and what was
/// typed handed back when the person leaves it. A box left empty hands back nothing, so an optional field stays unset.
/// </summary>
public class TextFieldTests
{
    /// <summary>What the field last handed back, and whether it did.</summary>
    private sealed class Holder
    {
        /// <summary>The value as last changed.</summary>
        public string? Value { get; set; }

        /// <summary>How many times the field handed a value back.</summary>
        public int Times { get; set; }
    }

    /// <summary>The field over this value.</summary>
    private static IRenderedComponent<TextField> Render(BunitContext ctx, Holder holder, string? value,
        bool multiline = false) =>
        ctx.Render<TextField>(parameters => parameters
            .Add(field => field.Label, "Summary")
            .Add(field => field.Value, value)
            .Add(field => field.Multiline, multiline)
            .Add(field => field.Hint, "Two or three sentences.")
            .Add(field => field.ValueChanged, changed =>
            {
                holder.Value = changed;
                holder.Times++;
            }));

    /// <summary>The label names the box, the box holds the value, and the hint sits under it.</summary>
    [Test]
    public void TheField_IsALabelOverItsBox_WithItsHint()
    {
        using var ctx = new BunitContext();

        var field = Render(ctx, new Holder(), "Builds software.");

        Assert.Multiple(() =>
        {
            Assert.That(field.Find("label .field-label").TextContent, Is.EqualTo("Summary"));
            Assert.That(field.Find("label input.field-box").GetAttribute("value"), Is.EqualTo("Builds software."));
            Assert.That(field.Find(".field-hint").TextContent, Is.EqualTo("Two or three sentences."));
        });
    }

    /// <summary>A field of several lines is a box that holds them.</summary>
    [Test]
    public void AMultilineField_IsATextArea()
    {
        using var ctx = new BunitContext();

        var field = Render(ctx, new Holder(), "Line one.", multiline: true);

        Assert.That(field.Find("label textarea.field-box").GetAttribute("value"), Is.EqualTo("Line one."));
    }

    /// <summary>What is typed is handed back, trimmed, when the person leaves the box.</summary>
    [Test]
    public void WhatIsTyped_IsHandedBack_Trimmed()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "Old");

        field.Find("input").Change("  New words ");

        Assert.That(holder.Value, Is.EqualTo("New words"));
    }

    /// <summary>
    /// Each keystroke is handed back as typed, so a preview beside the form keeps up, and the space between two
    /// words isn't taken away while the second is still to come.
    /// </summary>
    [Test]
    public void AKeystroke_IsHandedBack_AsTyped()
    {
        using var ctx = new BunitContext();
        var holder = new Holder();
        var field = Render(ctx, holder, "Old");

        field.Find("input").Input("New ");

        Assert.That(holder.Value, Is.EqualTo("New "));
    }

    /// <summary>A box left empty hands back nothing at all, not an empty word.</summary>
    [Test]
    public void AnEmptiedBox_HandsBackNothing()
    {
        using var ctx = new BunitContext();
        var holder = new Holder { Value = "Old" };
        var field = Render(ctx, holder, "Old");

        field.Find("input").Change("   ");

        Assert.Multiple(() =>
        {
            Assert.That(holder.Value, Is.Null);
            Assert.That(holder.Times, Is.EqualTo(1));
        });
    }
}
