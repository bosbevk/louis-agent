using LouisAgent.Site.Components.Cv;

namespace LouisAgent.Site.Tests.Components.Cv;

/// <summary>
/// F142: "Save as PDF" on the CV's page. One page, two outputs: the button opens the browser's own print dialog,
/// where saving as a PDF is a destination, and the page's print styles lay it out on A4. No PDF library.
/// </summary>
public class ResumePrintTests
{
    /// <summary>The button says what it does.</summary>
    [Test]
    public void TheButton_SaysWhatItDoes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var button = ctx.Render<ResumePrint>().Find("button.cv-print");

        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent.Trim(), Is.EqualTo("Save as PDF"));
            Assert.That(button.GetAttribute("type"), Is.EqualTo("button"));
        });
    }

    /// <summary>A click asks the browser to print, through the button's own script module.</summary>
    [Test]
    public void AClick_AsksTheBrowserToPrint()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ResumePrint.Script);
        module.SetupVoid("print").SetVoidResult();

        ctx.Render<ResumePrint>().Find("button.cv-print").Click();

        Assert.Multiple(() =>
        {
            Assert.That(ResumePrint.Script, Is.EqualTo("./Components/Cv/ResumePrint.razor.js"));
            Assert.That(module.Invocations.Select(call => call.Identifier), Is.EqualTo(new[] { "print" }));
            Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                    "src/site/louis-agent.site/Components/Cv/ResumePrint.razor.js")),
                Does.Contain("export function print()").And.Contain("window.print()"));
        });
    }
}
