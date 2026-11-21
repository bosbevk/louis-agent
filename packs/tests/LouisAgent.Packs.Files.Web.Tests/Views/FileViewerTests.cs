using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Packs.Files.Web.Views;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Files.Web.Tests.Views;

/// <summary>
/// The open file's highlighting (CL31): the viewer hands a newly opened file to the turn's module, the one that
/// highlights code on the site's highlight.js, and not to a global; the same file isn't done twice.
/// </summary>
public class FileViewerTests
{
    private static readonly WorkspaceFile App = new("src/app.cs", "app.cs", 5, "hello");

    [Test]
    public void AnOpenedFile_IsHandedToTheTurnsModule_ToHighlight()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.TurnView);
        module.SetupVoid("renderCode", _ => true).SetVoidResult();

        var viewer = ctx.Render<FileViewer>(p => p.Add(v => v.File, App));

        viewer.WaitForAssertion(() =>
            Assert.That(module.Invocations.Select(i => i.Identifier), Is.EqualTo(new[] { "renderCode" })));
        Assert.That(module.Invocations.Single().Arguments.Skip(1), Is.EqualTo(new object?[] { "hello", "csharp" }));
    }

    [Test]
    public void TheSameFile_IsNotHighlightedTwice()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.TurnView);
        module.SetupVoid("renderCode", _ => true).SetVoidResult();
        var viewer = ctx.Render<FileViewer>(p => p.Add(v => v.File, App));
        viewer.WaitForAssertion(() => Assert.That(module.Invocations, Has.Count.EqualTo(1)));

        viewer.Render();

        Assert.That(module.Invocations, Has.Count.EqualTo(1));
    }
}
