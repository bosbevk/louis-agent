using LouisAgent.Core.Web.Robots.Signals;

namespace LouisAgent.Core.Tests.Web.Robots.Signals;

/// <summary>
/// The no-AI signals a page sends on its own, in a meta tag or a response header. Written first:
/// <c>PageSignals.Read</c> threw NotImplementedException on the red run.
/// </summary>
public class PageSignalsTests
{
    private static readonly IReadOnlyDictionary<string, string> NoHeaders = new Dictionary<string, string>();

    /// <summary>A page whose head has one meta tag with the name and content you give.</summary>
    private static string PageWith(string name, string content) =>
        $"<html><head><title>x</title><meta name=\"{name}\" content=\"{content}\"></head><body>hi</body></html>";

    /// <summary>The headers of a response that sets just one header.</summary>
    private static IReadOnlyDictionary<string, string> Header(string name, string value) =>
        new Dictionary<string, string> { [name] = value };

    /// <summary>Drizzle and Dip's meta tag says noai, so we refuse and the refusal names the meta tag.</summary>
    [Test]
    public void ANoAiMeta_Refuses()
    {
        var signal = PageSignals.Read(NoHeaders, PageWith("robots", "noai, noimageai"));

        Assert.That(signal, Is.EqualTo(PageSignal.MetaNoAi));
    }

    /// <summary>noimageai alone is about pictures, not about reading the text, so we are let in.</summary>
    [Test]
    public void NoImageAiAlone_Allows()
    {
        Assert.That(PageSignals.Read(NoHeaders, PageWith("robots", "noimageai")), Is.Null);
    }

    /// <summary>
    /// The meta's name and content match whatever the case, the quotes or the order of the attributes.
    /// </summary>
    [TestCase("<META NAME=\"Robots\" CONTENT=\"NoAI\">")]
    [TestCase("<meta name='robots' content='index, noai'>")]
    [TestCase("<meta\n  content=\"noai\"\n  name=robots>")]
    public void TheMetaIsMatched_WhateverTheCaseQuotesOrOrder(string tag)
    {
        var signal = PageSignals.Read(NoHeaders, "<html><head>" + tag + "</head></html>");

        Assert.That(signal, Is.EqualTo(PageSignal.MetaNoAi));
    }

    /// <summary>A tag meant for Googlebot, or one that only looks like a robots tag, is not ours.</summary>
    [TestCase("<meta name=\"googlebot\" content=\"noai\">")]
    [TestCase("<meta name=\"robots-x\" content=\"noai\">")]
    [TestCase("<meta data-name=\"robots\" name=\"description\" content=\"noai\">")]
    [TestCase("<metadata name=\"robots\" content=\"noai\">")]
    public void ATagThatIsNotTheRobotsMeta_IsIgnored(string tag)
    {
        Assert.That(PageSignals.Read(NoHeaders, "<html><head>" + tag + "</head></html>"), Is.Null);
    }

    /// <summary>X-Robots-Tag with noai refuses, alone or in a list, and the header name is any case.</summary>
    [TestCase("noai")]
    [TestCase("noindex, noai")]
    public void XRobotsTagNoAi_Refuses(string value)
    {
        var signal = PageSignals.Read(Header("x-robots-tag", value), "<html></html>");

        Assert.That(signal, Is.EqualTo(PageSignal.XRobotsTagNoAi));
    }

    /// <summary>X-Robots-Tag about indexing says nothing about AI readers.</summary>
    [Test]
    public void XRobotsTagNoIndex_Allows()
    {
        Assert.That(PageSignals.Read(Header("X-Robots-Tag", "noindex, nofollow"), "<html></html>"), Is.Null);
    }

    /// <summary>Content-Signal ai-input=no refuses.</summary>
    [Test]
    public void ContentSignalAiInputNo_Refuses()
    {
        var signal = PageSignals.Read(Header("Content-Signal", "search=yes, ai-input=no"), null);

        Assert.That(signal, Is.EqualTo(PageSignal.ContentSignalAiInputNo));
    }

    /// <summary>Rooi Rose lets AI read and only says no to training; we read and don't train, so it allows.</summary>
    [Test]
    public void ContentSignalAiTrainNo_WithAiInputYes_Allows()
    {
        Assert.That(PageSignals.Read(Header("Content-Signal", "ai-input=yes, ai-train=no"), null), Is.Null);
        Assert.That(PageSignals.Read(Header("Content-Signal", "ai-train=no"), null), Is.Null);
    }

    /// <summary>tdm-reservation 1 reserves text-and-data-mining rights and refuses; 0 does not.</summary>
    [Test]
    public void TdmReservation_OneRefuses_ZeroAllows()
    {
        Assert.That(PageSignals.Read(Header("tdm-reservation", "1"), null), Is.EqualTo(PageSignal.TdmReservation));
        Assert.That(PageSignals.Read(Header("tdm-reservation", "0"), null), Is.Null);
    }

    /// <summary>A page with no signal at all, or no HTML to look in, allows.</summary>
    [Test]
    public void NoSignalAtAll_Allows()
    {
        Assert.That(PageSignals.Read(NoHeaders, "<html><head></head><body>plain</body></html>"), Is.Null);
        Assert.That(PageSignals.Read(NoHeaders, null), Is.Null);
    }
}
