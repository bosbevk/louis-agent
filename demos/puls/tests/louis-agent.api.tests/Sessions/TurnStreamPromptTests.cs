using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// The prompt the API builds from a message (F115): the visitor's words, then their attached files, then the page
/// they are reading as one block, cut at 6,000 characters with a note, so the model sees what the visitor sees.
/// </summary>
public class TurnStreamPromptTests
{
    /// <summary>A message with a page ends with the page's block: where the visitor is, and what the page says.</summary>
    [Test]
    public void APage_BecomesABlockAfterTheWords()
    {
        var request = new SendMessageRequest("which page am I on?", null, new PageRequest("/build-your-own", "Build your own", "A few lines of your own."));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Is.EqualTo("which page am I on?\n\n[The visitor is reading /build-your-own (Build your own). The page says:\nA few lines of your own.]"));
    }

    /// <summary>A long page is cut at 6,000 characters and the block says so.</summary>
    [Test]
    public void ALongPage_IsCut_AndSaysSo()
    {
        var request = new SendMessageRequest("hi", null, new PageRequest("/packs", "Packs", new string('x', 7_000)));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.Multiple(() =>
        {
            Assert.That(prompt, Does.Contain(new string('x', 6_000) + "]"));
            Assert.That(prompt, Does.Not.Contain(new string('x', 6_001)));
            Assert.That(prompt, Does.Contain("[Page truncated: showing 6,000 of 7,000 chars]"));
        });
    }

    /// <summary>A page's areas end the block (F117): each as its id and heading, one a line, so the model knows where it can send the visitor.</summary>
    [Test]
    public void ThePagesAreas_EndTheBlock()
    {
        var request = new SendMessageRequest("where are the levels?", null,
            new PageRequest("/identities", "Identities", "Who an agent is.", [new PageAreaRequest("parts", "What an identity holds"), new PageAreaRequest("levels", "The three that exist")]));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Does.EndWith("The page says:\nWho an agent is.\nIts areas:\n#parts What an identity holds\n#levels The three that exist]"));
    }

    /// <summary>Without a page the prompt is the words alone, as before.</summary>
    [Test]
    public void NoPage_LeavesThePromptAlone()
    {
        Assert.That(TurnStream.BuildPrompt(new SendMessageRequest("hi", null, null)), Is.EqualTo("hi"));
    }
}
