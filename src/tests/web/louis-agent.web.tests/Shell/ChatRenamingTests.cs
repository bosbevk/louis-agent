using LouisAgent.Web.Chats;
using LouisAgent.Web.Shell;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Shell;

/// <summary>
/// Renaming a chat, in the sidebar or the top bar: which chat, where the field is, what's typed, and what happens on Enter,
/// Escape or leaving the field.
/// </summary>
public class ChatRenamingTests
{
    /// <summary>Starting fills the field with the title, says where it is, and asks for the cursor once.</summary>
    [Test]
    public void Starting_FillsTheField_AndAsksForTheCursorOnce()
    {
        var (renaming, chat) = Make();

        renaming.Start(chat, inHeader: true);

        Assert.That(renaming.SessionId, Is.EqualTo("s"));
        Assert.That(renaming.InHeader, Is.True);
        Assert.That(renaming.Draft, Is.EqualTo("Old"));
        Assert.That(renaming.TakeFocus(), Is.True);
        Assert.That(renaming.TakeFocus(), Is.False);
    }

    /// <summary>Enter keeps the new name and closes the field.</summary>
    [Test]
    public async Task Enter_KeepsTheNewName()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);
        renaming.Draft = "  New  ";

        await renaming.KeyAsync("Enter", chat);

        Assert.That(chat.Title, Is.EqualTo("New"));
        Assert.That(renaming.SessionId, Is.Null);
    }

    /// <summary>Escape drops what was typed.</summary>
    [Test]
    public async Task Escape_DropsIt()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);
        renaming.Draft = "New";

        await renaming.KeyAsync("Escape", chat);

        Assert.That(chat.Title, Is.EqualTo("Old"));
        Assert.That(renaming.SessionId, Is.Null);
    }

    /// <summary>Any other key changes nothing.</summary>
    [Test]
    public async Task AnotherKey_ChangesNothing()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);

        await renaming.KeyAsync("a", chat);

        Assert.That(renaming.SessionId, Is.EqualTo("s"));
    }

    /// <summary>An empty name keeps the old one.</summary>
    [Test]
    public async Task AnEmptyName_KeepsTheOld()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);
        renaming.Draft = "   ";

        await renaming.FinishAsync(chat);

        Assert.That(chat.Title, Is.EqualTo("Old"));
    }

    /// <summary>A long name is cut at 80 characters.</summary>
    [Test]
    public async Task ALongName_IsCut()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);
        renaming.Draft = new string('x', 90);

        await renaming.FinishAsync(chat);

        Assert.That(chat.Title, Has.Length.EqualTo(80));
    }

    /// <summary>Leaving a field for another chat doesn't rename this one.</summary>
    [Test]
    public async Task FinishingAnotherChat_ChangesNothing()
    {
        var (renaming, chat) = Make();
        renaming.Start(chat, inHeader: false);
        renaming.Draft = "New";
        var other = new Chat { SessionId = "other", Title = "Other" };

        await renaming.FinishAsync(other);

        Assert.That(other.Title, Is.EqualTo("Other"));
        Assert.That(renaming.SessionId, Is.EqualTo("s"));
    }

    /// <summary>A renaming over a store whose browser answers with nothing, and a chat to rename.</summary>
    private static (ChatRenaming Renaming, Chat Chat) Make()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var store = new ChatStore(ctx.JSInterop.JSRuntime, new ChatSettings());
        return (new ChatRenaming(store), new Chat { SessionId = "s", Title = "Old" });
    }
}
