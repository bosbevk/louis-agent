using System.Runtime.CompilerServices;
using LouisAgent.Web.Scripts;
using System.Net;
using System.Text;
using LouisAgent.Web.Api;
using LouisAgent.Web.Shell;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Shell;

/// <summary>
/// The languages a new chat can speak: those the API has a voice for under the site's prefix. This browser's choice
/// wins if the API still offers it, then the site's default.
/// </summary>
public class ChatLanguagesTests
{
    private const string TwoVoices =
        """{"default":"site/voice-af","agent":["site/voice-af","site/voice-en","other/voice-de"],"specialist":[]}""";

    /// <summary>The languages are the API's voices under the site's prefix, and no others.</summary>
    /// <summary>The planned module, one per test context, so each test plans what the browser would answer.</summary>
    private static readonly ConditionalWeakTable<BunitContext, BunitJSModuleInterop> Modules = new();

    private static BunitJSModuleInterop Module(BunitContext ctx) =>
        Modules.GetValue(ctx, c => c.JSInterop.SetupModule(FrameworkScripts.Preferences));

    /// <summary>The preferences module as the shell builds it (CL31), over the test's interop.</summary>
    private static JsModule Preferences(BunitContext ctx) =>
        new(ctx.JSInterop.JSRuntime, FrameworkScripts.Preferences);

    [Test]
    public async Task TheLanguages_AreTheVoicesUnderThePrefix()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var languages = Make(ctx, TwoVoices);

        await languages.LoadAsync();

        Assert.That(languages.Available, Is.EqualTo(new[] { "af", "en" }));
    }

    /// <summary>With nothing chosen, it's the site's default.</summary>
    [Test]
    public async Task NothingChosen_IsTheDefault()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var languages = Make(ctx, TwoVoices);

        await languages.LoadAsync();

        Assert.That(languages.Current, Is.EqualTo("af"));
        Assert.That(languages.Role, Is.EqualTo("site/voice-af"));
    }

    /// <summary>This browser's choice wins, if the API still offers it.</summary>
    [TestCase("en", "en")]
    [TestCase("fr", "af")]
    public async Task TheBrowsersChoice_Wins_IfStillOffered(string saved, string expected)
    {
        using var ctx = new BunitContext();
        Module(ctx).Setup<string?>("loadLanguage").SetResult(saved);
        var languages = Make(ctx, TwoVoices);

        await languages.LoadAsync();

        Assert.That(languages.Current, Is.EqualTo(expected));
    }

    /// <summary>If the API doesn't have the default, the first it has is used.</summary>
    [Test]
    public async Task NoDefaultOnTheApi_UsesTheFirst()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var languages = Make(ctx, """{"default":"x","agent":["site/voice-en"],"specialist":[]}""");

        await languages.LoadAsync();

        Assert.That(languages.Current, Is.EqualTo("en"));
    }

    /// <summary>An API that can't say keeps the site's default.</summary>
    [Test]
    public async Task AnApiThatCantSay_KeepsTheDefault()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var languages = Make(ctx, "", HttpStatusCode.NotFound);

        await languages.LoadAsync();

        Assert.That(languages.Available, Is.EqualTo(new[] { "af" }));
        Assert.That(languages.Current, Is.EqualTo("af"));
    }

    /// <summary>Choosing keeps the language, and this browser remembers it.</summary>
    [Test]
    public async Task Choosing_KeepsIt_AndTheBrowserRemembers()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var languages = Make(ctx, TwoVoices);

        await languages.ChooseAsync("en");

        Assert.That(languages.Role, Is.EqualTo("site/voice-en"));
        Assert.That(ctx.JSInterop.Invocations["saveLanguage"].Single().Arguments[0], Is.EqualTo("en"));
    }

    /// <summary>The languages over an API that answers the roles with this.</summary>
    private static ChatLanguages Make(BunitContext ctx, string roles, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(roles, Encoding.UTF8, "application/json")
        });
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());
        return new ChatLanguages(api, Preferences(ctx), "site/voice-", "af");
    }
}
