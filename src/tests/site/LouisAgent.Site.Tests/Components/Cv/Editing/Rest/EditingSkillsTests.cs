using LouisAgent.Site.Resumes.Skills;
using LouisAgent.Site.Tests.Admin.Forms;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing.Rest;

/// <summary>
/// F142, step 7c: the skills are changed where they stand. A group's name is clicked and retyped, its skills are
/// tags to add and take out, and the groups are a list.
/// </summary>
public class EditingSkillsTests
{
    private static readonly IReadOnlyList<ResumeSkill> Skills = TheCv.Resume.Skills;

    /// <summary>A group's name is retyped, its skills kept.</summary>
    [Test]
    public void AGroupsName_IsRetyped()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();

        OnThePage.Editable(ctx, handed).Retype("#skills .panel:nth-child(1) h3", "Languages I write");

        Assert.That(handed.Value!.Skills[0], Is.EqualTo(Skills[0] with { Name = "Languages I write" }));
    }

    /// <summary>A skill is taken out of its group by the cross on its tag.</summary>
    [Test]
    public void ASkill_IsTakenOut_ByItsTag()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("#skills .panel:nth-child(1) .tag-remove").Click();

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Skills[0].Keywords, Is.EqualTo(Skills[0].Keywords.Skip(1)));
            Assert.That(handed.Value.Skills.Skip(1), Is.EqualTo(Skills.Skip(1)));
        });
    }

    /// <summary>A group moves a step, one is taken out, and one is added at the end.</summary>
    [Test]
    public void TheGroups_AreAList()
    {
        using var ctx = new BunitContext();
        var moved = new Handed<CvRecord>();
        var removed = new Handed<CvRecord>();
        var added = new Handed<CvRecord>();

        OnThePage.Editable(ctx, moved).Find("#skills button[aria-label='Move group 2 earlier']").Click();
        OnThePage.Editable(ctx, removed).Find("#skills button[aria-label='Remove group 1']").Click();
        OnThePage.Editable(ctx, added).Find("#skills button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(moved.Value!.Skills.Take(2), Is.EqualTo(new[] { Skills[1], Skills[0] }));
            Assert.That(removed.Value!.Skills, Is.EqualTo(Skills.Skip(1)));
            Assert.That(added.Value!.Skills, Has.Count.EqualTo(Skills.Count + 1));
        });
    }
}
