using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints;

/// <summary>
/// A browser that follows the confirmation link lands on the web's page, done or failed; a client that asks for
/// JSON gets Identity's own answer. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class ConfirmationRedirectTests
{
    private const string Database = "puls_accounts_test_confirm";
    private AccountsTestHost _host = null!;

    [OneTimeSetUp]
    public async Task StartWithARegistration()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database);
        using var registered = await _host.Client.PostAsJsonAsync("/account/register",
            new
            {
                email = "ann@example.com",
                password = "Long-enough-1"
            });
        registered.EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    /// <summary>A request for HTML, with a browser's Accept header.</summary>
    private static HttpRequestMessage AsBrowser(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        return request;
    }

    [Test, Order(1)]
    public async Task ABadLink_SendsABrowserToThePage_Failed()
    {
        using var plain = _host.PlainClient();
        string bad = _host.Mail.Links.Single().Replace("code=", "code=x");

        using var response = await plain.SendAsync(AsBrowser(bad));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Found));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("/confirm-email?failed=true"));
        });
    }

    [Test, Order(2)]
    public async Task TheLink_SendsABrowserToThePage_Done()
    {
        using var plain = _host.PlainClient();

        using var response = await plain.SendAsync(AsBrowser(_host.Mail.Links.Single()));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Found));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("/confirm-email?done=true"));
        });
    }

    /// <summary>A client that doesn't ask for HTML gets Identity's own text, as the test host's sign-up relies on.</summary>
    [Test, Order(3)]
    public async Task AClientThatAsksForNoHtml_GetsIdentitysAnswer()
    {
        using var plain = _host.PlainClient();

        using var response = await plain.GetAsync(_host.Mail.Links.Single());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.Unauthorized),
            "confirmed already, so Identity says yes or no as it likes; never a redirect");
    }
}
