using LouisAgent.Composition.Words;
using LouisAgent.Site.Content.Privacy;
using LouisAgent.Site.Content.Privacy.Parts;
using LouisAgent.Site.Content.Trust;

namespace LouisAgent.Site.Tests.Content.Privacy;

/// <summary>
/// The privacy page says what is kept as the code keeps it (F43.1): a chat is kept in Microsoft Azure for 30 days after
/// its last message, a photo or a PDF never; its words come from a dictionary factory (ADR 0132). Nothing on the site
/// still says a chat lives only in memory.
/// </summary>
public class PrivacyWordsTests
{
    /// <summary>The tiles are a dictionary factory over the page's own parts.</summary>
    [Test]
    public void WhatIsKept_IsAPartWordsOverItsOwnParts()
    {
        var type = typeof(PrivacyWords).GetField(nameof(PrivacyWords.WhatIsKept))!.FieldType;

        Assert.Multiple(() =>
        {
            Assert.That(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PartWords<,>), Is.True);
            Assert.That(type.GetGenericArguments()[0].Namespace, Is.EqualTo("LouisAgent.Site.Content.Privacy.Parts"));
        });
    }

    /// <summary>
    /// What you type is kept in Azure for 30 days after your last message; photos and PDFs never are.
    /// </summary>
    [Test]
    public void WhatYouType_IsKept30Days_InAzure_ButNoPhotoOrPdf()
    {
        string text = PrivacyWords.WhatIsKept[PrivacyPart.WhatYouType].Text;

        Assert.That(text, Does.Contain("30 days after your last message")
            .And.Contain("Microsoft Azure")
            .And.Contain("Photos and PDFs you send are never kept")
            .And.Not.Contain("memory"));
    }

    /// <summary>No fact on the trust page still says a chat is held only in memory, or isn't stored.</summary>
    [Test]
    public void NoTrustFact_SaysChatsArentKept() =>
        Assert.That(TrustFacts.All.Select(fact => fact.Text),
            Has.None.Contains("in memory").And.None.Contains("aren't stored"));

    /// <summary>Azure, in the table of who handles data, is where the chats are kept.</summary>
    [Test]
    public void Azure_KeepsTheChats_InTheTableOfHandlers()
    {
        var azure = DataHandlers.Rows.Single(row => row.Topic == "Microsoft Azure");

        Assert.That(azure.Cells[1], Does.Contain("Your chats, for 30 days"));
    }
}
