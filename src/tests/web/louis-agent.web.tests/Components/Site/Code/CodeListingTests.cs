using LouisAgent.Web.Components.Site.Code;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Code;

/// <summary>
/// F160.1: a page shows code as a listing named for its file or its language, the code itself as text, so a page that
/// shows a program can't run or break the page with it.
/// </summary>
public class CodeListingTests
{
    /// <summary>The code is shown as text, never read as markup, under its name.</summary>
    [Test]
    public void TheCode_IsText_UnderItsName()
    {
        using var ctx = Context();

        var cut = ctx.Render<CodeListing>(listing => listing
            .Add(l => l.Name, "Program.cs")
            .Add(l => l.Language, "csharp")
            .Add(l => l.Code, "var x = new List<string>(); // <b>not bold</b>"));

        var code = cut.Find("figure.code-listing pre code");
        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("figure.code-listing figcaption span").TextContent.Trim(), Is.EqualTo("Program.cs"));
            Assert.That(code.TextContent, Is.EqualTo("var x = new List<string>(); // <b>not bold</b>"));
            Assert.That(code.QuerySelectorAll("b"), Is.Empty, "markup in the code stays text");
            Assert.That(code.ClassList, Does.Contain("language-csharp"));
        });
    }

    /// <summary>
    /// A listing can be copied as it stands (Louis: "this must be an actual code block you can copy paste"): its button
    /// hands the code, exactly as shown, to its module, and then says it was copied.
    /// </summary>
    [Test]
    public void ItsButton_CopiesTheCode_AndSaysSo()
    {
        using var ctx = new BunitContext();
        var text = new ChatText();
        ctx.Services.AddSingleton(text);
        const string program = "await CliHost.RunAsync(args);";
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.CodeListing);
        module.Setup<bool>("copy", program).SetResult(true);

        var cut = ctx.Render<CodeListing>(listing => listing
            .Add(l => l.Name, "Program.cs")
            .Add(l => l.Language, "csharp")
            .Add(l => l.Code, program));
        cut.Find("figure.code-listing button.code-copy").Click();

        Assert.Multiple(() =>
        {
            Assert.That(module.Invocations["copy"].Single().Arguments, Is.EqualTo(new object?[] { program }));
            cut.WaitForAssertion(() =>
                Assert.That(cut.Find("button.code-copy").TextContent.Trim(), Is.EqualTo(text.Copied)));
        });
    }

    /// <summary>With no name, the listing is named for its language.</summary>
    [Test]
    public void WithNoName_TheLanguageNamesIt()
    {
        using var ctx = Context();

        var cut = ctx.Render<CodeListing>(listing => listing
            .Add(l => l.Language, "yaml")
            .Add(l => l.Code, "name: louis-agent"));

        Assert.That(cut.Find("figcaption span").TextContent.Trim(), Is.EqualTo("yaml"));
    }

    /// <summary>A context a listing draws in: the site's words, and its module answering anything.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }
}
