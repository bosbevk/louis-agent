using System.Reflection;
using LouisAgent.Web.Accounts.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Tests.Pages;

/// <summary>
/// F164.1: the accounts have a public part and a part behind a sign-in (Louis: "/Login / register / password-reset
/// etc is public"). A page behind a sign-in says so with Blazor's own <c>[Authorize]</c>, so the host never draws it
/// for a search engine; every other page is public and drawn.
/// </summary>
public class PublicPagesTests
{
    /// <summary>Of the account pages, only the profile is behind a sign-in.</summary>
    [Test]
    public void OnlyTheProfile_IsBehindASignIn()
    {
        var pages = AccountLibrary.Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any()).ToList();

        var behindASignIn = pages.Where(page => page.GetCustomAttribute<AuthorizeAttribute>() is not null);

        Assert.Multiple(() =>
        {
            Assert.That(pages, Has.Count.GreaterThanOrEqualTo(6), "the account pages were found");
            Assert.That(behindASignIn, Is.EqualTo(new[] { typeof(ProfilePage) }));
        });
    }
}
