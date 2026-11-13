using System.Reflection;
using LouisAgent.Site.Admin;
using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Layout;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Admin;

/// <summary>
/// F142, Louis: "can we have a resume area for the admin section where I can edit my resume in a userfriendly
/// manner?", then "can we not just edit it inline?". The editor is the CV's own page inside the admin, with a small
/// form beside it for what the page doesn't draw. It opens on the copy the API holds, keeps every edit in a draft,
/// and sends the draft only when he saves.
/// </summary>
public class ResumeEditorTests
{
    /// <summary>The summary on the editor's sheet, drawn by the page's own view.</summary>
    private const string SheetSummary = ".resume-sheet .cv-hero .lede";

    /// <summary>The CV with this headline, which the page never draws.</summary>
    private static Resume With(string headline) =>
        Cv.Resume with { Basics = Cv.Resume.Basics with { Label = headline } };

    /// <summary>The editor, once it has the API's copy.</summary>
    private static IRenderedComponent<ResumeEditor> Open(BunitContext ctx)
    {
        var editor = ctx.Render<ResumeEditor>();
        editor.WaitForAssertion(() => Assert.That(editor.FindAll(".resume-editor"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        return editor;
    }

    /// <summary>It is a page of the admin, inside its frame, so it sits behind the admin's key.</summary>
    [Test]
    public void TheEditor_IsAPageOfTheAdmin()
    {
        var page = typeof(ResumeEditor);

        Assert.Multiple(() =>
        {
            Assert.That(page.GetCustomAttribute<RouteAttribute>()!.Template, Is.EqualTo("/admin/resume"));
            Assert.That(page.GetCustomAttribute<LayoutAttribute>()!.LayoutType, Is.EqualTo(typeof(AdminFrame)));
        });
    }

    /// <summary>
    /// FX153, Louis, on louis-agent.dev: "this should be behind the key it is not". Without the admin's key the
    /// editor does not open: no page, no form, only the place to enter the key. The CV is public, but its editor
    /// is the admin's.
    /// </summary>
    [Test]
    public void WithoutTheKey_TheEditorDoesNotOpen()
    {
        var api = new CvApi(Cv.Resume) { Locked = true };
        using var ctx = api.Context();

        var editor = ctx.Render<ResumeEditor>();
        editor.WaitForAssertion(() => Assert.That(editor.FindAll(".admin-gate"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindAll(".admin-gate input[type=password]"), Has.Count.EqualTo(1),
                "the key is asked for");
            Assert.That(editor.FindAll(".resume-editor, .resume-sheet, .field-box, button.draft-save"), Is.Empty);
        });
    }

    /// <summary>Once a key the API takes is saved, the editor opens, with no reload of the page.</summary>
    [Test]
    public void OnceAKeyIsSaved_TheEditorOpens()
    {
        var api = new CvApi(Cv.Resume) { Locked = true };
        using var ctx = api.Context();
        var editor = ctx.Render<ResumeEditor>();
        editor.WaitForAssertion(() => Assert.That(editor.FindAll(".admin-gate"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        api.Locked = false;
        editor.Find(".admin-gate input[type=password]").Change("the-admin-key");
        editor.Find(".admin-gate form").Submit();

        editor.WaitForAssertion(() => Assert.That(editor.FindAll(".resume-editor"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(editor.FindAll(".admin-gate"), Is.Empty);
    }

    /// <summary>It opens on the copy the API holds, drawn as the page draws it, with nothing to save.</summary>
    [Test]
    public void TheEditor_OpensOnTheApisCopy_DrawnAsThePage()
    {
        var api = new CvApi(With("Kept by the API"));
        using var ctx = api.Context();

        var editor = Open(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(editor.Find(SheetSummary).TextContent, Is.EqualTo(Cv.Resume.Basics.Summary));
            Assert.That(editor.Find(".resume-sheet img.cv-photo").GetAttribute("src"), Is.EqualTo(Cv.Photo));
            Assert.That(editor.Box("Headline").GetAttribute("value"), Is.EqualTo("Kept by the API"));
            Assert.That(editor.Find(".draft-state").TextContent, Is.EqualTo("No changes"));
            Assert.That(editor.Find("button.draft-save").HasAttribute("disabled"), Is.True);
        });
    }

    /// <summary>An edit is kept in the draft, to be saved or dropped, and nothing reaches the site yet.</summary>
    [Test]
    public void AnEdit_IsADraft_UntilItIsSaved()
    {
        var api = new CvApi(Cv.Resume);
        using var ctx = api.Context();
        var editor = Open(ctx);

        editor.Box("Headline").Input("Typed just now");

        Assert.Multiple(() =>
        {
            Assert.That(editor.Find(".draft-state").TextContent, Is.EqualTo("Unsaved changes"));
            Assert.That(editor.Find("button.draft-save").HasAttribute("disabled"), Is.False);
            Assert.That(editor.Find("button.draft-discard").HasAttribute("disabled"), Is.False);
            Assert.That(api.Saves, Is.Empty, "nothing reaches the site until it is saved");
        });
    }

    /// <summary>
    /// Step 7b: the page in the editor is the editor. His name is clicked and retyped where it stands, the page shows
    /// the new name at once, and it is a draft like any other edit.
    /// </summary>
    [Test]
    public void WordsOnThePage_AreRetypedWhereTheyStand()
    {
        var api = new CvApi(Cv.Resume);
        using var ctx = api.Context();
        var editor = Open(ctx);

        editor.Find(".resume-sheet h1 .inline-read").Click();
        editor.Find(".resume-sheet h1 .inline-box").Input("Louis J Marais");
        editor.Find(".resume-sheet h1 .inline-box").KeyDown(Key.Enter);

        Assert.Multiple(() =>
        {
            Assert.That(editor.Find(".resume-sheet h1").TextContent, Is.EqualTo("Louis J Marais"));
            Assert.That(editor.Find(".draft-state").TextContent, Is.EqualTo("Unsaved changes"));
            Assert.That(api.Saves, Is.Empty);
        });
    }

    /// <summary>Save sends the whole draft to the API, and what the API kept is then the saved CV.</summary>
    [Test]
    public void Save_SendsTheDraft()
    {
        var api = new CvApi(Cv.Resume);
        using var ctx = api.Context();
        var editor = Open(ctx);
        editor.Box("Headline").Input("Saved from the admin");

        editor.Find("button.draft-save").Click();
        editor.WaitForAssertion(() => Assert.That(editor.Find(".draft-state").TextContent, Does.StartWith("Saved")),
            TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(api.Saves, Has.Count.EqualTo(1));
            Assert.That(ResumeReader.Read(api.Saves[0]).Basics.Label, Is.EqualTo("Saved from the admin"));
            Assert.That(ResumeReader.Read(api.Saves[0]).Work, Has.Count.EqualTo(Cv.Resume.Work.Count), "the whole CV");
            Assert.That(api.Held.Basics.Label, Is.EqualTo("Saved from the admin"));
            Assert.That(editor.Find("button.draft-save").HasAttribute("disabled"), Is.True);
        });
    }

    /// <summary>A save the API refuses says why, in the API's words, and the draft is still there to fix.</summary>
    [Test]
    public void ARefusedSave_SaysWhy_AndKeepsTheDraft()
    {
        var api = new CvApi(Cv.Resume) { Refusal = "The CV holds an ID number." };
        using var ctx = api.Context();
        var editor = Open(ctx);
        editor.Box("Headline").Input("Not kept yet");

        editor.Find("button.draft-save").Click();
        editor.WaitForAssertion(() => Assert.That(editor.FindAll(".admin-gate"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(editor.Find(".admin-gate").TextContent, Does.Contain("The CV holds an ID number."));
            Assert.That(editor.Box("Headline").GetAttribute("value"), Is.EqualTo("Not kept yet"));
            Assert.That(editor.Find(".draft-state").TextContent, Is.EqualTo("Unsaved changes"));
        });
    }

    /// <summary>Discard puts the saved CV back.</summary>
    [Test]
    public void Discard_PutsTheSavedCvBack()
    {
        var api = new CvApi(With("As saved"));
        using var ctx = api.Context();
        var editor = Open(ctx);
        editor.Box("Headline").Input("A change of mind");

        editor.Find("button.draft-discard").Click();

        Assert.Multiple(() =>
        {
            Assert.That(editor.Box("Headline").GetAttribute("value"), Is.EqualTo("As saved"));
            Assert.That(editor.Find(".draft-state").TextContent, Is.EqualTo("No changes"));
        });
    }
}
