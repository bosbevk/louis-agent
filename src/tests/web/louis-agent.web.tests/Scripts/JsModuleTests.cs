using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Scripts;

/// <summary>
/// The module proxy (CL31): it imports its module once, however many calls come, forwards each call to the module's
/// function with its arguments, and disposes what it loaded and nothing when it never did.
/// </summary>
public class JsModuleTests
{
    private const string Path = "./_content/louis-agent.web/js/example.js";

    [Test]
    public async Task ItImportsOnce_AndForwardsEachCall()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(Path);
        module.SetupVoid("focus", "#box").SetVoidResult();
        module.Setup<bool>("copy", "hello").SetResult(true);
        var proxy = new JsModule(ctx.JSInterop.JSRuntime, Path);

        await proxy.InvokeVoidAsync("focus", "#box");
        bool copied = await proxy.InvokeAsync<bool>("copy", "hello");

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.True);
            Assert.That(ctx.JSInterop.Invocations.Count(i => i.Identifier == "import"), Is.EqualTo(1));
            Assert.That(module.Invocations.Select(i => i.Identifier), Is.EqualTo(new[] { "focus", "copy" }));
        });
    }

    [Test]
    public async Task RacingCalls_ShareOneImport()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(Path);
        module.SetupVoid("focus", _ => true).SetVoidResult();
        var proxy = new JsModule(ctx.JSInterop.JSRuntime, Path);

        await Task.WhenAll(proxy.InvokeVoidAsync("focus", "#a").AsTask(), proxy.InvokeVoidAsync("focus", "#b").AsTask());

        Assert.That(ctx.JSInterop.Invocations.Count(i => i.Identifier == "import"), Is.EqualTo(1));
    }

    [Test]
    public async Task Disposing_LetsALoadedModuleGo_AndImportsNothingForOneNeverUsed()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = ctx.JSInterop.SetupModule(Path);
        module.SetupVoid("focus", "#box").SetVoidResult();
        var used = new JsModule(ctx.JSInterop.JSRuntime, Path);
        var unused = new JsModule(ctx.JSInterop.JSRuntime, Path);
        await used.InvokeVoidAsync("focus", "#box");

        await used.DisposeAsync();
        await unused.DisposeAsync();

        Assert.That(ctx.JSInterop.Invocations.Count(i => i.Identifier == "import"), Is.EqualTo(1),
            "the unused one imported nothing, and strict mode would have refused anything unplanned");
    }

    [Test]
    public void ThePaths_AreUnderTheLibrarysContent() =>
        Assert.That(new[]
            {
                FrameworkScripts.Shell, FrameworkScripts.MessageList, FrameworkScripts.TurnView,
                FrameworkScripts.Preferences, FrameworkScripts.Dialogs, FrameworkScripts.Download
            },
            Has.All.StartWith("./_content/louis-agent.web/").And.All.EndsWith(".js"));
}
