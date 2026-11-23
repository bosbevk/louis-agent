using LouisAgent.Web.Evals.Components.Marks;

namespace LouisAgent.Web.Evals.Tests.Components.Marks;

/// <summary>A mark is a tick or a cross a screen reader says in words, never a colour alone.</summary>
public class MarkDotTests
{
    /// <summary>A met mark is a tick that says "met".</summary>
    [Test]
    public void AMetMark_IsATick_ThatSaysMet()
    {
        using var ctx = new BunitContext();
        var dot = ctx.Render<MarkDot>(parameters => parameters.Add(p => p.Met, true));

        Assert.That(dot.Find(".mark-dot").ClassList, Does.Contain("mark-met"));
        Assert.That(dot.Find(".visually-hidden").TextContent, Is.EqualTo("met"));
    }

    /// <summary>A mark not met is a cross that says "not met".</summary>
    [Test]
    public void AMarkNotMet_IsACross_ThatSaysNotMet()
    {
        using var ctx = new BunitContext();
        var dot = ctx.Render<MarkDot>(parameters => parameters.Add(p => p.Met, false));

        Assert.That(dot.Find(".mark-dot").ClassList, Does.Contain("mark-not-met"));
        Assert.That(dot.Find(".visually-hidden").TextContent, Is.EqualTo("not met"));
    }
}
