using LouisAgent.Site.Components.Cv;
using LouisAgent.Site.Tests.Admin.Forms;
using Microsoft.AspNetCore.Components.Web;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing;

/// <summary>Louis's CV drawn with someone listening for its change, and a person's hands on it.</summary>
internal static class OnThePage
{
    /// <summary>The CV's view, handing every change to the holder.</summary>
    public static IRenderedComponent<ResumeView> Editable(BunitContext ctx, Handed<CvRecord> handed)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<ResumeView>(parameters => parameters
            .Add(view => view.Resume, TheCv.Resume)
            .Add(view => view.Photo, TheCv.Photo)
            .Add(view => view.Availability, TheCv.Availability)
            .Add(view => view.ResumeChanged, changed => handed.Value = changed));
    }

    /// <summary>Clicks the words found there, types these in their place, and keeps them with Enter.</summary>
    public static void Retype(this IRenderedComponent<ResumeView> view, string where, string words)
    {
        view.Find($"{where} .inline-read").Click();
        view.Find($"{where} .inline-box").Input(words);
        view.Find($"{where} .inline-box").KeyDown(Key.Enter);
    }

    /// <summary>The same for a longer text, where Ctrl with Enter keeps.</summary>
    public static void RetypeLong(this IRenderedComponent<ResumeView> view, string where, string words)
    {
        var keep = new KeyboardEventArgs
        {
            Key = "Enter",
            CtrlKey = true
        };
        view.Find($"{where} .inline-read").Click();
        view.Find($"{where} .inline-box").Input(words);
        view.Find($"{where} .inline-box").KeyDown(keep);
    }
}
