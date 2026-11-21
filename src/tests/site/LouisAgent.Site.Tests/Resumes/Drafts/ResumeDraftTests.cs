using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes.Drafts;

namespace LouisAgent.Site.Tests.Resumes.Drafts;

/// <summary>
/// F142: the CV being edited in the admin is a draft beside the saved one (Louis's pick of three: a form per section
/// with a live preview). The form edits the draft, the preview draws it, and nothing reaches the site until Save.
/// </summary>
public class ResumeDraftTests
{
    /// <summary>A draft starts as the saved CV, with nothing to save.</summary>
    [Test]
    public void ADraft_StartsAsTheSavedCv()
    {
        var draft = new ResumeDraft(Cv.Resume);

        Assert.Multiple(() =>
        {
            Assert.That(draft.Current, Is.SameAs(Cv.Resume));
            Assert.That(draft.Changed, Is.False);
        });
    }

    /// <summary>An edit changes the draft and not the saved CV.</summary>
    [Test]
    public void AnEdit_ChangesTheDraft_NotTheSaved()
    {
        var draft = new ResumeDraft(Cv.Resume);

        draft.Edit(Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Edited" } });

        Assert.Multiple(() =>
        {
            Assert.That(draft.Current.Basics.Label, Is.EqualTo("Edited"));
            Assert.That(draft.Saved.Basics.Label, Is.EqualTo(Cv.Resume.Basics.Label));
            Assert.That(draft.Changed, Is.True);
        });
    }

    /// <summary>An edit that puts the words back the way they were leaves nothing to save.</summary>
    [Test]
    public void AnEditBackToTheSame_LeavesNothingToSave()
    {
        var draft = new ResumeDraft(Cv.Resume);
        draft.Edit(Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Edited" } });

        draft.Edit(Cv.Resume with { Basics = Cv.Resume.Basics with { Label = Cv.Resume.Basics.Label } });

        Assert.That(draft.Changed, Is.False);
    }

    /// <summary>Discard puts the draft back to the saved CV.</summary>
    [Test]
    public void Discard_PutsTheDraftBack()
    {
        var draft = new ResumeDraft(Cv.Resume);
        draft.Edit(Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Edited" } });

        draft.Discard();

        Assert.Multiple(() =>
        {
            Assert.That(draft.Current.Basics.Label, Is.EqualTo(Cv.Resume.Basics.Label));
            Assert.That(draft.Changed, Is.False);
        });
    }

    /// <summary>Once the API has kept it, what it answered is the saved CV, and there is nothing left to save.</summary>
    [Test]
    public void Kept_MakesTheApisAnswerTheSavedCv()
    {
        var draft = new ResumeDraft(Cv.Resume);
        var edited = Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Edited" } };
        draft.Edit(edited);

        draft.Kept(edited);

        Assert.Multiple(() =>
        {
            Assert.That(draft.Saved.Basics.Label, Is.EqualTo("Edited"));
            Assert.That(draft.Changed, Is.False);
        });
    }
}
