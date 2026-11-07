using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Scripts;

/// <summary>
/// The chat frame's modules (CL31): each is built once, on first use; disposing takes the page's listeners down only
/// when the shell's module ever loaded, and lets go only what loaded.
/// </summary>
public class ChatModulesTests
{
    [Test]
    public async Task Disposing_DetachesTheShell_AndLetsGoOnlyWhatLoaded()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var shell = ctx.JSInterop.SetupModule(FrameworkScripts.Shell);
        shell.SetupVoid("attach").SetVoidResult();
        shell.SetupVoid("detach").SetVoidResult();
        ctx.JSInterop.SetupModule(FrameworkScripts.Dialogs);
        var modules = new ChatModules(ctx.JSInterop.JSRuntime);
        await modules.Shell.InvokeVoidAsync("attach");

        await modules.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(shell.Invocations.Select(i => i.Identifier), Is.EqualTo(new[] { "attach", "detach" }));
            Assert.That(ctx.JSInterop.Invocations.Count(i => i.Identifier == "import"), Is.EqualTo(1),
                "the dialogs and the preferences were never asked for, so nothing was imported or let go for them");
        });
    }

    [Test]
    public async Task NeverUsed_DisposingTalksToNoBrowser()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;

        await new ChatModules(ctx.JSInterop.JSRuntime).DisposeAsync();

        Assert.That(ctx.JSInterop.Invocations, Is.Empty);
    }

    [Test]
    public void EachModule_IsTheSameOneEveryTime()
    {
        using var ctx = new BunitContext();
        var modules = new ChatModules(ctx.JSInterop.JSRuntime);
        var shell = modules.Shell;

        Assert.Multiple(() =>
        {
            Assert.That(modules.Shell, Is.SameAs(shell));
            Assert.That(modules.Preferences.Path, Is.EqualTo(FrameworkScripts.Preferences));
            Assert.That(modules.Dialogs.Path, Is.EqualTo(FrameworkScripts.Dialogs));
        });
    }
}
