using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Components.Admin;
using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Shared.Tests.Components.Admin;

/// <summary>
/// F59: what an admin page shows when the API said no. It always says what went wrong, asks for the key only when the
/// key was what was refused, and hands a saved key back to the page to read again.
/// </summary>
public class AdminGateTests
{
    /// <summary>The gate, over a browser store that answers with nothing, and where a saved key lands.</summary>
    private static (IRenderedComponent<AdminGate> Gate, ChatSettings Settings) Render(BunitContext ctx, bool needsKey,
        Action? onSaved = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));

        var gate = ctx.Render<AdminGate>(p => p
            .Add(c => c.Error, "The API said no.")
            .Add(c => c.NeedsKey, needsKey)
            .Add(c => c.OnSaved, EventCallback.Factory.Create(new object(), onSaved ?? (() => { }))));
        return (gate, settings);
    }

    /// <summary>A failure that isn't about the key says what went wrong, and asks for nothing.</summary>
    [Test]
    public void AnotherFailure_SaysWhatWentWrong_AndAsksForNothing()
    {
        using var ctx = new BunitContext();

        var (gate, _) = Render(ctx, needsKey: false);

        Assert.That(gate.Find(".admin-gate").TextContent, Does.Contain("The API said no."));
        Assert.That(gate.FindAll(".admin-key"), Is.Empty);
    }

    /// <summary>A refused key shows the box to enter one.</summary>
    [Test]
    public void ARefusedKey_ShowsTheBox()
    {
        using var ctx = new BunitContext();

        var (gate, _) = Render(ctx, needsKey: true);

        Assert.That(gate.Find(".admin-gate").TextContent, Does.Contain("The API said no."));
        Assert.That(gate.Find(".admin-key input").GetAttribute("type"), Is.EqualTo("password"));
    }

    /// <summary>A saved key lands in the settings, the box empties, and the page is told to read again.</summary>
    [Test]
    public void SavingAKey_KeepsIt_AndTellsThePage()
    {
        using var ctx = new BunitContext();
        int told = 0;
        var (gate, settings) = Render(ctx, needsKey: true, () => told++);

        gate.Find(".admin-key input").Change("  the-key  ");
        gate.Find(".admin-key").Submit();

        Assert.That(settings.AdminKey, Is.EqualTo("the-key"));
        Assert.That(told, Is.EqualTo(1));
        Assert.That(gate.Find(".admin-key input").GetAttribute("value"), Is.Empty.Or.Null);
    }
}
