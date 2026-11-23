using LouisAgent.Api.Site.Rendering;
using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.About;
using LouisAgent.Site.Pages.Agents;
using LouisAgent.SiteApi;
using LouisAgent.SiteApi.Resumes;

namespace LouisAgent.Site.Tests.Pages.Meta;

/// <summary>
/// The pages a business reads are named for what it buys (F128.1, Louis: "the page names must change now"): the
/// title a browser tab and a search result show, as the site's host serves it.
/// </summary>
public class PageTitlesTests
{
    /// <summary>The page comes out of the host's prerenderer, with the services the host gives it.</summary>
    private static async Task<string> TitleOf(Type page, string address)
    {
        // The host's own list (FX155), with no folder for the CV, so it is the built-in one.
        var prerenderer = new PagePrerenderer(services => SitePageServices.Register(services, new ResumeStore(null)));

        var rendered = await prerenderer.RenderAsync(page, new Uri(new Uri("https://louis-agent.dev"), address));

        return rendered!.Title;
    }

    /// <summary>The landing, the demos and the contact page sell.</summary>
    [TestCase(typeof(HomePage), "/", "Websites with an agent · louis-agent")]
    [TestCase(typeof(DemosPage), "/demos", "See an agent work · louis-agent")]
    [TestCase(typeof(ContactPage), "/contact", "Ask for yours · louis-agent")]
    [TestCase(typeof(PrivacyPage), "/privacy", "Your privacy · louis-agent")]
    [TestCase(typeof(TrustPage), "/trust", "Trust and security · louis-agent")]
    public async Task ABusinessPage_IsNamedForWhatItSells(Type page, string address, string title) =>
        Assert.That(await TitleOf(page, address), Is.EqualTo(title));
}
