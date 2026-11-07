using System.Net;
using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.Mail;
using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Accounts.Tests;

/// <summary>
/// Without a database the three calls are a quiet no: the host runs, and no account route exists. With one, the
/// routes refuse to map until the middleware is in.
/// </summary>
public class AccountsStartupTests
{
    private static WebApplication Build(AccountsOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddLouisAgentAccounts(options);
        return builder.Build();
    }

    [Test]
    public async Task WithNoDatabase_TheHostRuns_AndHasNoAccountRoute()
    {
        var app = Build(new AccountsOptions(null, "site", null));
        app.UseLouisAgentAccounts();
        app.MapLouisAgentAccounts();
        await app.StartAsync();

        using var response = await app.GetTestClient().PostAsync("/account/register", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await app.StopAsync();
    }

    /// <summary>A program that forgot the middleware is told so before it listens, not by a wrong tenant later.</summary>
    [Test]
    public void WithADatabase_MappingBeforeUse_Refuses()
    {
        var app = Build(new AccountsOptions("Server=nowhere;Encrypt=true", "site", null));

        var refused = Assert.Throws<InvalidOperationException>(() => app.MapLouisAgentAccounts());

        Assert.That(refused!.Message, Does.StartWith("Call UseLouisAgentAccounts before MapLouisAgentAccounts"));
    }

    /// <summary>The mail seam resolves to the log sender, and the store to ours, from the services alone.</summary>
    [Test]
    public void WithADatabase_TheServicesAreOurs()
    {
        var app = Build(new AccountsOptions("Server=nowhere;Encrypt=true", "home", null));
        using var scope = app.Services.CreateScope();

        Assert.Multiple(() =>
        {
            Assert.That(scope.ServiceProvider.GetRequiredService<IEmailSender<AccountUser>>(),
                Is.TypeOf<LogEmailSender>());
            Assert.That(scope.ServiceProvider.GetRequiredService<IUserStore<AccountUser>>(),
                Is.TypeOf<AccountUserStore>());
            Assert.That(scope.ServiceProvider.GetRequiredService<ITenantResolver>(), Is.TypeOf<HostTenantResolver>());
        });
    }
}
