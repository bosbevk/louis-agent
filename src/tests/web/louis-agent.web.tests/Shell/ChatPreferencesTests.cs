using System.Runtime.CompilerServices;
using LouisAgent.Web.Scripts;
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
    /// <summary>The planned module, one per test context, so each test plans what the browser would answer.</summary>
    private static readonly ConditionalWeakTable<BunitContext, BunitJSModuleInterop> Modules = new();

    private static BunitJSModuleInterop Module(BunitContext ctx) =>
        Modules.GetValue(ctx, c => c.JSInterop.SetupModule(FrameworkScripts.Preferences));

    /// <summary>The preferences module as the shell builds it (CL31), over the test's interop.</summary>
    private static JsModule Preferences(BunitContext ctx) =>
        new(ctx.JSInterop.JSRuntime, FrameworkScripts.Preferences);

    [Test]
    public async Task Loading_ReadsWhatTheBrowserKept()
    {
        using var ctx = new BunitContext();
        Module(ctx).Setup<string?>("loadLocation").SetResult("Stellenbosch");
        Module(ctx).Setup<bool>("motion").SetResult(true);
        Module(ctx).Setup<bool>("reducedMotion").SetResult(true);
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());

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
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());

        await preferences.SaveLocationAsync("  Paarl ");

        Assert.That(preferences.Location, Is.EqualTo("Paarl"));
        Assert.That(ctx.JSInterop.Invocations["saveLocation"].Single().Arguments[0], Is.EqualTo("Paarl"));
    }

    /// <summary>An empty place clears it, rather than keeping a blank.</summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task AnEmptyPlace_ClearsIt(string? typed)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());
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
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());

        await preferences.SaveMotionAsync(true);

        Assert.That(preferences.Motion, Is.True);
        Assert.That(ctx.JSInterop.Invocations["setMotion"].Single().Arguments[0], Is.EqualTo(true));
    }

    /// <summary>A place the browser found is kept, and it says so in the site's words.</summary>
    [Test]
    public async Task AFoundPlace_IsKept_AndSaysSo()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Module(ctx).Setup<LocatedPlace>("locate", _ => true).SetResult(new LocatedPlace("Worcester", null));
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());

        string said = await preferences.UseMyLocationAsync();

        Assert.That(preferences.Location, Is.EqualTo("Worcester"));
        Assert.That(preferences.Locating, Is.False);
        Assert.That(said, Is.EqualTo("Location: Worcester"));
    }

    /// <summary>When the script names a problem, the site's words say it, and the place stays.</summary>
    [TestCase("denied", "You didn't give permission for your location.")]
    [TestCase("no-browser", "This browser can't give your location.")]
    [TestCase("not-named", "OpenStreetMap couldn't name the place.")]
    [TestCase("unreachable", "Couldn't reach OpenStreetMap.")]
    [TestCase("not-found", "Couldn't find your location.")]
    public async Task AProblem_IsSaidInTheSitesWords_AndThePlaceStays(string problem, string words)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Module(ctx).Setup<LocatedPlace>("locate", _ => true).SetResult(new LocatedPlace(null, problem));
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());
        await preferences.SaveLocationAsync("Paarl");

        string said = await preferences.UseMyLocationAsync();

        Assert.That(preferences.Location, Is.EqualTo("Paarl"));
        Assert.That(said, Is.EqualTo(words));
    }

    /// <summary>The site says which languages the place is named in.</summary>
    [Test]
    public async Task TheSitesLanguages_GoToTheScript()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText { PlaceLanguages = "xx,en" });

        try
        {
            await preferences.UseMyLocationAsync();
        }
        catch (NullReferenceException)
        {
            // Loose interop answers with nothing; only the call matters here.
        }

        Assert.That(ctx.JSInterop.Invocations["locate"].Single().Arguments, Is.EqualTo(new object[] { "xx,en" }));
    }

    /// <summary>With no place and no reason, it says it couldn't find one.</summary>
    [Test]
    public async Task NoPlaceAndNoReason_SaysItCouldntFindOne()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Module(ctx).Setup<LocatedPlace>("locate", _ => true).SetResult(new LocatedPlace(null, null));
        var preferences = new ChatPreferences(Preferences(ctx), new ChatText());

        string said = await preferences.UseMyLocationAsync();

        Assert.That(said, Is.EqualTo("Couldn't find your location."));
    }
}
