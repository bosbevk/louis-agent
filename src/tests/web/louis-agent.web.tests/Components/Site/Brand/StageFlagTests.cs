using LouisAgent.Web.Components.Site.Brand;

namespace LouisAgent.Web.Tests.Components.Site.Brand;

/// <summary>
/// FX170: a flag that says what stage a site is at, beside its name, and opens a short note on a press (Louis: "flag in
/// the header version: alpha disclosure", "running version alpha-1 may experience inconsistencies").
/// </summary>
public class StageFlagTests
{
    /// <summary>
    /// FX179: the flag keeps its words on one line, since on a phone, beside the name and the header's buttons, it
    /// broke into "alpha-" over "1".
    /// </summary>
    [Test]
    public void TheFlag_StaysOnOneLine()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Brand/StageFlag.razor.scss"));
        int flag = styles.IndexOf(".stage-flag {", StringComparison.Ordinal);

        Assert.That(styles[flag..styles.IndexOf('}', flag)], Does.Contain("white-space: nowrap;"));
    }

    /// <summary>The flag is a press that opens its note, the browser's own popover, so nothing needs a script.</summary>
    [Test]
    public void TheFlag_OpensItsNote_AsAPopover()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<StageFlag>(p => p
            .Add(f => f.Label, "alpha-1")
            .Add(f => f.Note, "This is version alpha-1.")
            .Add(f => f.LinkText, "Tell us what you find").Add(f => f.Href, "/contact"));
        var press = cut.Find("button.stage-flag");
        var note = cut.Find(".stage-note");

        Assert.Multiple(() =>
        {
            Assert.That(press.TextContent.Trim(), Is.EqualTo("alpha-1"));
            Assert.That(press.GetAttribute("popovertarget"), Is.EqualTo(note.Id));
            Assert.That(note.HasAttribute("popover"), Is.True);
            Assert.That(note.TextContent, Does.Contain("This is version alpha-1."));
            Assert.That(note.QuerySelector("a")!.GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }
}
