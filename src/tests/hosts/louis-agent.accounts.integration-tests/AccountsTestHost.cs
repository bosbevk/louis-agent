using System.Net.Http.Json;
using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// The accounts as <c>AccountsStartup</c> builds them, on a test server with no port and a database of the fixture's
/// own: the same services, middleware and routes a demo's program gets. The mail goes to <see cref="Mail"/>, so a
/// test can follow a confirmation link, and the client keeps the sign-in cookie between calls.
/// </summary>
internal sealed class AccountsTestHost : IAsyncDisposable
{
    public WebApplication App { get; }
    public HttpClient Client { get; }

    /// <summary>What was mailed, newest last: the link or code of each message.</summary>
    public CapturingEmailSender Mail { get; }

    private AccountsTestHost(WebApplication app, HttpClient client, CapturingEmailSender mail)
    {
        App = app;
        Client = client;
        Mail = mail;
    }

    /// <summary>Starts the host on the fixture's database, in this default tenant.</summary>
    public static async Task<AccountsTestHost> StartAsync(string database, string defaultTenant = "home",
        Action<WebApplication>? map = null)
    {
        var address = new SqlConnectionStringBuilder(TestDatabase.Url) { InitialCatalog = database };
        var mail = new CapturingEmailSender();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddLouisAgentAccounts(new AccountsOptions(address.ConnectionString, defaultTenant, null));
        builder.Services.AddSingleton<IEmailSender<AccountUser>>(mail);
        var app = builder.Build();
        app.UseLouisAgentAccounts();
        app.MapLouisAgentAccounts();
        map?.Invoke(app);
        await app.StartAsync();

        return new AccountsTestHost(app,
            new HttpClient(new CookieHandler(app.GetTestServer().CreateHandler()))
            {
                BaseAddress = app.GetTestServer().BaseAddress
            }, mail);
    }

    /// <summary>Registers, follows the confirmation link and signs in, so a test starts as a signed-in person.</summary>
    public async Task SignUpAsync(string email, string password)
    {
        using var registered = await Client.PostAsJsonAsync("/account/register", new
        {
            email,
            password
        });
        registered.EnsureSuccessStatusCode();
        using var confirmed = await Client.GetAsync(Mail.Links.Last());
        confirmed.EnsureSuccessStatusCode();
        using var signedIn = await Client.PostAsJsonAsync("/account/login?useCookies=true", new
        {
            email,
            password
        });
        signedIn.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
    }

    /// <summary>Sends the cookies the server set, as a browser would; the test server's client doesn't.</summary>
    private sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly System.Net.CookieContainer _jar = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string cookies = _jar.GetCookieHeader(request.RequestUri!);
            if (cookies.Length > 0) request.Headers.Add("Cookie", cookies);
            var response = await base.SendAsync(request, cancellationToken);
            if (response.Headers.TryGetValues("Set-Cookie", out var set))
                foreach (string header in set)
                    _jar.SetCookies(request.RequestUri!, header);
            return response;
        }
    }
}
