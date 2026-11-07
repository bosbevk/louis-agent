using System.Net;
using System.Text.Json;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Components.Passkeys;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Components.Passkeys;

/// <summary>
/// The passkey list and the passkey sign-in, with the browser's part scripted: the options go from the API to the
/// browser, the credential from the browser to the API, and a browser that can't do passkeys sees no form.
/// </summary>
public class PasskeyComponentsTests
{
    private const string Options = """{"challenge":"abc","rp":{"id":"localhost"}}""";
    private const string Credential = """{"id":"cred-1","type":"public-key"}""";

    private static (BunitContext Context, StubHandler Handler) Context(bool supported, string listJson)
    {
        var (ctx, handler) = AccountTestContext.New(request => Answer(request, listJson));
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        ctx.JSInterop.Setup<bool>("passkeys.supported").SetResult(supported);
        ctx.JSInterop.Setup<string>("passkeys.create", Options).SetResult(Credential);
        ctx.JSInterop.Setup<string>("passkeys.get", Options).SetResult(Credential);
        return (ctx, handler);
    }

    /// <summary>The API, scripted: who I am, the list, either kind of options, and a plain yes to the rest.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request, string listJson)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/me", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann);
        if (path.EndsWith("options", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK, Options);
        if (request.Method == HttpMethod.Get) return AccountTestContext.Reply(HttpStatusCode.OK, listJson);
        return AccountTestContext.Reply(HttpStatusCode.NoContent);
    }

    [Test]
    public void TheList_ShowsEachPasskeyByName_AndAddsOne_ThroughTheBrowser()
    {
        var (ctx, handler) = Context(true,
            """{"passkeys":[{"id":"cred-0","name":"Laptop","createdAt":"2026-11-07T10:00:00Z"}]}""");
        using var _ = ctx;
        var cut = ctx.Render<PasskeyList>();
        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-logins li span").TextContent, Is.EqualTo("Laptop")));

        cut.Find("input[type=text]").Change("Phone");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("is added")));
        var posted = handler.Requests.Single(r =>
            r.Url.EndsWith("/account/passkeys", StringComparison.Ordinal) && r.Method == HttpMethod.Post);
        Assert.That(JsonSerializer.Deserialize<PasskeyBody>(posted.Body!, AccountJson.Web),
            Is.EqualTo(new PasskeyBody(Credential, "Phone")),
            "the credential goes to the API as the browser wrote it, under the name given");
    }

    [Test]
    public void ABrowserThatCannot_SeesNoForm()
    {
        var (ctx, _) = Context(false, """{"passkeys":[]}""");
        using var __ = ctx;

        var cut = ctx.Render<PasskeyList>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("form"), Is.Empty);
            Assert.That(cut.Find("p.muted").TextContent, Does.Contain("can't do passkeys"));
        });
    }

    [Test]
    public void TheSignIn_GetsTheOptions_TheCredential_AndSignsIn()
    {
        var (ctx, handler) = Context(true, "");
        using var _ = ctx;
        bool signedIn = false;
        var cut = ctx.Render<PasskeySignIn>(p => p.Add(s => s.OnSignedIn, () => signedIn = true));
        cut.WaitForElement("button");

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.That(signedIn, Is.True));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Select(r => r.Url), Does
                .Contain("http://localhost/account/passkeys/request-options")
                .And.Contain("http://localhost/account/passkeys/login"));
            Assert.That(ctx.Services.GetRequiredService<AccountState>().SignedIn, Is.True);
        });
    }
}
