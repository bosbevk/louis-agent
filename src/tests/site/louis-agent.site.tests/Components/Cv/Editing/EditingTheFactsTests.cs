using LouisAgent.Site.Resumes;
using LouisAgent.Site.Tests.Admin.Forms;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing;

/// <summary>
/// F142, step 7b: the facts under the CV's top are changed where they stand. A figure and its line are clicked and
/// retyped, and each fact has buttons to move it a step or take it out, with one under the row to add another.
/// Every change comes back as the whole CV with that one thing changed.
/// </summary>
public class EditingTheFactsTests
{
    private static readonly IReadOnlyList<ResumeFact> Facts = TheCv.Resume.Facts;

    /// <summary>A figure is clicked and retyped, the other facts as they were.</summary>
    [Test]
    public void AFigure_IsRetypedWhereItStands()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Retype(".cv-fact:nth-child(1) b", "21 years");

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Facts[0], Is.EqualTo(Facts[0] with { Value = "21 years" }));
            Assert.That(handed.Value.Facts.Skip(1), Is.EqualTo(Facts.Skip(1)));
            Assert.That(handed.Value.Basics, Is.SameAs(TheCv.Resume.Basics), "nothing else in the CV is touched");
        });
    }

    /// <summary>The line under a figure is retyped too.</summary>
    [Test]
    public void AFactsLine_IsRetypedWhereItStands()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.RetypeLong(".cv-fact:nth-child(2) span", "pull requests, each reviewed");

        Assert.That(handed.Value!.Facts[1], Is.EqualTo(Facts[1] with { Label = "pull requests, each reviewed" }));
    }

    /// <summary>A fact moves a step later, changing places with the next.</summary>
    [Test]
    public void AFact_MovesAStep()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("button[aria-label='Move fact 1 later']").Click();

        Assert.That(handed.Value!.Facts.Take(2), Is.EqualTo(new[] { Facts[1], Facts[0] }));
    }

    /// <summary>A fact is taken out.</summary>
    [Test]
    public void AFact_IsTakenOut()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("button[aria-label='Remove fact 2']").Click();

        Assert.That(handed.Value!.Facts, Is.EqualTo(Facts.Where(fact => fact != Facts[1])));
    }

    /// <summary>A fact is added at the end of the row, empty, to be clicked and typed.</summary>
    [Test]
    public void AFact_IsAdded_AtTheEnd()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find(".cv-facts button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Facts, Has.Count.EqualTo(Facts.Count + 1));
            Assert.That(handed.Value.Facts[^1], Is.EqualTo(new ResumeFact("", "")));
        });
    }
}
