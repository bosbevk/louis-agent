using LouisAgent.Web.Shell;
using LouisAgent.Web.Text;

namespace LouisAgent.Web.Tests.Shell;

/// <summary>
/// What this browser remembers about the person: where they are, and whether the page may move. The page's script keeps
/// them, so nothing leaves the browser until a new chat sends the place.
/// </summary>
public class ChatPreferencesTests
{
    /// <summary>Loading reads what this browser kept.</summary>
    [Test]
    public async Task Loading_ReadsWhatTheBrowserKept()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Setup<string?>("chatUi.loadLocation").SetResult("Stellenbosch");
        ctx.JSInterop.Setup<bool>("chatUi.motion").SetResult(true);
        ctx.JSInterop.Setup<bool>("chatUi.reducedMotion").SetResult(true);
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());

        await preferences.LoadAsync();

        Assert.That(preferences.Location, Is.EqualTo("Stellenbosch"));
        Assert.That(preferences.Motion, Is.True);
        Assert.That(preferences.ReducedMotion, Is.True);
    }

    /// <summary>A typed place is trimmed and kept.</summary>
    [Test]
    public async Task ATypedPlace_IsTrimmed_AndKept()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());

        await preferences.SaveLocationAsync("  Paarl ");

        Assert.That(preferences.Location, Is.EqualTo("Paarl"));
        Assert.That(ctx.JSInterop.Invocations["chatUi.saveLocation"].Single().Arguments[0], Is.EqualTo("Paarl"));
    }

    /// <summary>An empty place clears it, rather than keeping a blank.</summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task AnEmptyPlace_ClearsIt(string? typed)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());
        await preferences.SaveLocationAsync("Paarl");

        await preferences.SaveLocationAsync(typed);

        Assert.That(preferences.Location, Is.Null);
    }

    /// <summary>Motion is kept, and told to the page.</summary>
    [Test]
    public async Task Motion_IsKept()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());

        await preferences.SaveMotionAsync(true);

        Assert.That(preferences.Motion, Is.True);
        Assert.That(ctx.JSInterop.Invocations["chatUi.setMotion"].Single().Arguments[0], Is.EqualTo(true));
    }

    /// <summary>A place the browser found is kept, and it says so in the site's words.</summary>
    [Test]
    public async Task AFoundPlace_IsKept_AndSaysSo()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<LocatedPlace>("chatUi.locate").SetResult(new LocatedPlace("Worcester", null));
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());

        string said = await preferences.UseMyLocationAsync();

        Assert.That(preferences.Location, Is.EqualTo("Worcester"));
        Assert.That(preferences.Locating, Is.False);
        Assert.That(said, Is.EqualTo("Location: Worcester"));
    }

    /// <summary>When the browser gives a reason, that's what it says, and the place stays.</summary>
    [Test]
    public async Task AReason_IsSaid_AndThePlaceStays()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<LocatedPlace>("chatUi.locate").SetResult(new LocatedPlace(null, "Permission denied"));
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());
        await preferences.SaveLocationAsync("Paarl");

        string said = await preferences.UseMyLocationAsync();

        Assert.That(preferences.Location, Is.EqualTo("Paarl"));
        Assert.That(said, Is.EqualTo("Permission denied"));
    }

    /// <summary>With no place and no reason, it says it couldn't find one.</summary>
    [Test]
    public async Task NoPlaceAndNoReason_SaysItCouldntFindOne()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<LocatedPlace>("chatUi.locate").SetResult(new LocatedPlace(null, null));
        var preferences = new ChatPreferences(ctx.JSInterop.JSRuntime, new ChatText());

        string said = await preferences.UseMyLocationAsync();

        Assert.That(said, Is.EqualTo("Couldn't find your location."));
    }
}
