using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Web.Evals.Components.EvalsPage.Details;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Evals.Tests.Components.EvalsPage.Details;

/// <summary>
/// The manual's dialog takes the focus when it opens, through the framework's dialogs module (CL31), so Esc reaches it;
/// without the module it still shows the manual.
/// </summary>
public class ManualDialogTests
{
    private static readonly ManualReply Manual = new("cook", "The cook", "# How it grades\n\nOne meal.");

    [Test]
    public void OnOpening_ItTakesTheFocus_ThroughTheDialogsModule()
    {
        using var ctx = new BunitContext();
        var dialogs = ctx.JSInterop.SetupModule(FrameworkScripts.Dialogs);
        dialogs.SetupVoid("focus", "#manual-dialog").SetVoidResult();

        var cut = ctx.Render<ManualDialog>(p => p.Add(d => d.Manual, Manual));

        cut.WaitForAssertion(() =>
            Assert.That(dialogs.Invocations.Any(i => i.Identifier == "focus"), Is.True, "the focus was asked for"));
        Assert.That(cut.Markup, Does.Contain("How it grades"));
    }

    [Test]
    public void WithoutTheModule_ItStillShowsTheManual()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<ManualDialog>(p => p.Add(d => d.Manual, Manual));

        Assert.That(cut.Markup, Does.Contain("The cook"));
    }
}
