using LouisAgent.Core.Agent;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// F97.1 step 9: a chat keeps the names of the skills it drafted, so only it finds them; another chat starts with none.
/// </summary>
public class ConversationDraftsTests
{
    /// <summary>A chat's drafts are the ones it made, each once; a new chat has none.</summary>
    [Test]
    public void AChatsDrafts_AreTheOnesItMade()
    {
        var chat = new Conversation([]);
        var other = new Conversation([]);

        chat.Drafted("word-count");
        chat.Drafted("line-count");
        chat.Drafted("word-count");

        Assert.Multiple(() =>
        {
            Assert.That(chat.Drafts, Is.EquivalentTo(new[] { "word-count", "line-count" }));
            Assert.That(other.Drafts, Is.Empty);
        });
    }
}
