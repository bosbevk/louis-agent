using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Components.Editing;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing;

/// <summary>
/// F142, step 7b: the links under the summary are a list changed on the page. While it is edited a link is no
/// longer a way off the page. It has buttons to move it, take it out and open it, and it opens on a small form for
/// what its button doesn't show: the network, the name on it and the address behind it.
/// </summary>
public class EditingTheLinksTests
{
    private static readonly IReadOnlyList<ResumeProfile> Links = TheCv.Resume.Basics.Profiles;

    /// <summary>While the page is edited, a click on a link doesn't leave it: the buttons lead nowhere.</summary>
    [Test]
    public void WhileEditing_ALinkLeadsNowhere()
    {
        using var ctx = new BunitContext();

        var view = OnThePage.Editable(ctx, new Handed<CvRecord>());

        Assert.Multiple(() =>
        {
            Assert.That(view.FindAll(".cv-reach a[href]"), Is.Empty);
            Assert.That(view.FindAll(".cv-reach a.btn"), Has.Count.EqualTo(Links.Count), "still drawn as the buttons");
        });
    }

    /// <summary>A link opens on its three fields, and the tick keeps a changed address in its place.</summary>
    [Test]
    public void ALink_OpensOnItsFields_AndKeepsAChangedAddress()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("button[aria-label='Change link 2']").Click();
        var fields = view.FindComponents<TextField>();
        var labels = fields.Select(field => field.Instance.Label).ToList();
        fields.Single(field => field.Instance.Label == "Address").Find(".field-box").Input("https://example.org/me");
        view.Find(".inline-form button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Is.EqualTo(new[] { "Network", "Name on it", "Address" }));
            Assert.That(handed.Value!.Basics.Profiles[1], Is.EqualTo(Links[1] with { Url = "https://example.org/me" }));
            Assert.That(handed.Value.Basics.Profiles[0], Is.EqualTo(Links[0]));
            Assert.That(view.FindAll(".inline-form"), Is.Empty, "kept, and closed");
        });
    }

    /// <summary>The cross closes a link's form and changes nothing.</summary>
    [Test]
    public void TheCross_ClosesALinksForm_AndChangesNothing()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);
        view.Find("button[aria-label='Change link 1']").Click();

        view.Find(".inline-form button.inline-drop").Click();

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value, Is.Null);
            Assert.That(view.FindAll(".inline-form"), Is.Empty);
        });
    }

    /// <summary>A link moves a step earlier, and one is taken out.</summary>
    [Test]
    public void ALink_MovesAStep_AndIsTakenOut()
    {
        using var ctx = new BunitContext();
        var moved = new Handed<CvRecord>();
        var removed = new Handed<CvRecord>();

        OnThePage.Editable(ctx, moved).Find("button[aria-label='Move link 2 earlier']").Click();
        OnThePage.Editable(ctx, removed).Find("button[aria-label='Remove link 1']").Click();

        Assert.Multiple(() =>
        {
            Assert.That(moved.Value!.Basics.Profiles.Take(2), Is.EqualTo(new[] { Links[1], Links[0] }));
            Assert.That(removed.Value!.Basics.Profiles, Is.EqualTo(Links.Skip(1)));
        });
    }

    /// <summary>A link is added at the end, empty, for its form to fill in.</summary>
    [Test]
    public void ALink_IsAdded_AtTheEnd()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find(".cv-reach button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Basics.Profiles, Has.Count.EqualTo(Links.Count + 1));
            Assert.That(handed.Value.Basics.Profiles[^1], Is.EqualTo(new ResumeProfile("", "")));
        });
    }
}
