using LouisAgent.Web.Components.Chat.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Chat.Shell;

/// <summary>
/// How a Review or Ask button on an admin screen reaches a chat: it opens the site's chat with the words as a draft.
/// Nothing here can send them, so the person always reads and sends it themselves.
/// </summary>
public class ChatHandoffTests
{
    /// <summary>It goes to the address the site gave, and the chat finds the draft there.</summary>
    [Test]
    public void OpeningAChat_GoesToTheSitesChat_WithTheDraft()
    {
        using var ctx = new BunitContext();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var handoff = new ChatHandoff("gesels");

        handoff.OpenChatWith("Review this", navigation);

        Assert.That(navigation.Uri, Does.EndWith("/gesels"));
        Assert.That(handoff.Take(), Is.EqualTo("Review this"));
    }

    /// <summary>A draft is taken once, so coming back to the chat later doesn't fill the box again.</summary>
    [Test]
    public void ADraft_IsTakenOnce()
    {
        using var ctx = new BunitContext();
        var handoff = new ChatHandoff("");
        handoff.OpenChatWith("Review this", ctx.Services.GetRequiredService<NavigationManager>());

        handoff.Take();

        Assert.That(handoff.Take(), Is.Null);
    }

    /// <summary>With nothing handed over there's nothing to take.</summary>
    [Test]
    public void WithNothingHandedOver_ThereIsNothingToTake() =>
        Assert.That(new ChatHandoff("").Take(), Is.Null);
}
