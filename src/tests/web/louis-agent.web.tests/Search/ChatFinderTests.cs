using LouisAgent.Web.Search;
using LouisAgent.Web.Chats;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Search;

/// <summary>
/// The search over the chats saved in this browser, by rule: an empty query is the recent chats, a title match shows no
/// snippet, a match in a message or an answer shows the line around it, at most twenty.
/// </summary>
public class ChatFinderTests
{
    /// <summary>A chat with this title and these messages.</summary>
    private static ChatModel Chat(string title, params string[] texts) => new()
    {
        SessionId = title,
        Title = title,
        Turns = texts.Select(t => new Turn { UserText = t, Blocks = [new Block { Kind = BlockKind.Text, Text = "Antwoord: " + t }] }).ToList(),
    };

    /// <summary>An empty query is the recent chats, up to eight.</summary>
    [Test]
    public void AnEmptyQuery_IsTheRecentChats_UpToEight()
    {
        var chats = Enumerable.Range(1, 10).Select(i => Chat($"gesprek {i}")).ToList();

        var found = ChatFinder.Find(chats, "  ");

        Assert.That(found.Select(f => f.Chat.Title), Is.EqualTo(chats.Take(8).Select(c => c.Title)));
        Assert.That(found.All(f => f.Snippet is null), Is.True);
    }

    /// <summary>A title match has no snippet; a message match shows the line.</summary>
    [Test]
    public void ATitleMatch_HasNoSnippet_AMessageMatch_ShowsTheLine()
    {
        var chats = new List<ChatModel> { Chat("Melktert resep", "hoe bak ek dit?"), Chat("Ander", "Ek soek 'n melktert vir Sondag"), Chat("Niks", "brood") };

        var found = ChatFinder.Find(chats, "MELKTERT");

        Assert.That(found.Select(f => f.Chat.Title), Is.EqualTo(new[] { "Melktert resep", "Ander" }));
        Assert.That(found[0].Snippet, Is.Null);
        Assert.That(found[1].Snippet, Is.EqualTo("Ek soek 'n melktert vir Sondag"));
    }

    /// <summary>An answer is searched too, and no more than twenty chats come back.</summary>
    [Test]
    public void AnAnswer_IsSearchedToo_AndNoMoreThanTwentyChats()
    {
        var chats = Enumerable.Range(1, 25).Select(i => Chat($"gesprek {i}", "vraag")).ToList();

        Assert.That(ChatFinder.Find(chats, "Antwoord"), Has.Count.EqualTo(20));
    }

    /// <summary>A snippet is about eighty characters around the match, with ellipses.</summary>
    [Test]
    public void ASnippet_IsAboutEightyCharactersAroundTheMatch_WithEllipses()
    {
        string text = new string('a', 50) + "\nmelktert" + new string('b', 100);

        string? snippet = ChatFinder.Snippet(text, "Melktert");

        Assert.That(snippet, Does.StartWith("…").And.EndWith("…"));
        Assert.That(snippet, Does.Contain(" melktert"), "the line break became a space");
        Assert.That(snippet, Has.Length.EqualTo(82));
        Assert.That(ChatFinder.Snippet("niks hier nie", "melktert"), Is.Null);
    }
}
