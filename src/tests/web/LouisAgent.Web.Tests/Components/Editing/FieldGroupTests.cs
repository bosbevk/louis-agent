using LouisAgent.Web.Components.Editing;

namespace LouisAgent.Web.Tests.Components.Editing;

/// <summary>
/// F142: a few fields of a form that belong together, under a small heading: how to reach someone, where they live.
/// The group is a fieldset, so a screen reader says its heading before each field in it.
/// </summary>
public class FieldGroupTests
{
    /// <summary>A group with two fields in it, under this heading.</summary>
    private static IRenderedComponent<FieldGroup> Render(BunitContext ctx, string? title) =>
        ctx.Render<FieldGroup>(parameters => parameters
            .Add(group => group.Title, title)
            .AddChildContent<TextField>(field => field.Add(box => box.Label, "Email"))
            .AddChildContent<TextField>(field => field.Add(box => box.Label, "Phone")));

    /// <summary>The heading is the fieldset's legend, and the fields sit inside it.</summary>
    [Test]
    public void AGroup_IsAFieldset_WithItsHeadingAndItsFields()
    {
        using var ctx = new BunitContext();

        var group = Render(ctx, "Reach");

        Assert.Multiple(() =>
        {
            Assert.That(group.Find("fieldset.field-group > legend.field-group-title").TextContent, Is.EqualTo("Reach"));
            Assert.That(group.FindAll("fieldset.field-group .field-group-fields label.field"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>A group with no heading draws none, not an empty one.</summary>
    [Test]
    public void AGroupWithNoHeading_DrawsNone()
    {
        using var ctx = new BunitContext();

        var group = Render(ctx, title: null);

        Assert.That(group.FindAll("legend"), Is.Empty);
    }
}
