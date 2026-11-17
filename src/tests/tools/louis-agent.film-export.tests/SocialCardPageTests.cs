using LouisAgent.FilmExport.Cards;

namespace LouisAgent.FilmExport.Tests;

/// <summary>
/// FX174: the share card is drawn from a page whose glass orb is the one the site draws, photographed from it (Louis:
/// "where is my glass?"); the page's template takes the photograph in each place the orb stands.
/// </summary>
public class SocialCardPageTests
{
    /// <summary>Every place for the orb takes its photograph, as a data address, and no placeholder is left.</summary>
    [Test]
    public void TheOrb_StandsInEachPlace_AndNoPlaceholderIsLeft()
    {
        const string template = "<body class=\"{look}\"><img src=\"{orb}\"><p>louis-agent</p><img class=\"big\" src=\"{orb}\">";
        byte[] orb = [1, 2, 3];

        string page = SocialCardPage.Fill(template, orb, CardLook.Light);

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Not.Contain("{orb}").And.Not.Contain("{look}"));
            Assert.That(page.Split("data:image/png;base64,AQID").Length - 1, Is.EqualTo(2));
            // The card's look, light or dark, is a class its page reads (Louis: "and the light theme one?").
            Assert.That(page, Does.Contain("<body class=\"light\">"));
        });
    }
}
