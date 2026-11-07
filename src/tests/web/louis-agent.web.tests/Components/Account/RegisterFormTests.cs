using System.Net;
using LouisAgent.Web.Components.Account;

namespace LouisAgent.Web.Tests.Components.Account;

/// <summary>The registration form checks the passwords agree, registers, and lists what the server refused.</summary>
public class RegisterFormTests
{
    [Test]
    public void PasswordsThatDiffer_AreRefusedHere_BeforeAnyCall()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var cut = ctx.Render<RegisterForm>();

        cut.Find("input[type=email]").Change("ann@x.test");
        var passwords = cut.FindAll("input[type=password]");
        passwords[0].Input("Long-enough-1");
        passwords[1].Input("Different-2");
        cut.Find("form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".account-notice.is-error").TextContent, Does.Contain("aren't the same"));
            Assert.That(handler.Requests, Is.Empty);
        });
    }

    [Test]
    public void ARegistration_PostsTheEmailAndPassword_AndTellsThePageTheEmail()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        string? registered = null;
        var cut = ctx.Render<RegisterForm>(p => p.Add(form => form.OnRegistered, email => registered = email));

        cut.Find("input[type=email]").Change(" ann@x.test ");
        foreach (var password in cut.FindAll("input[type=password]")) password.Input("Long-enough-1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(registered, Is.EqualTo("ann@x.test")));
        Assert.That(handler.Requests.Single().Body,
            Is.EqualTo("""{"email":"ann@x.test","password":"Long-enough-1"}"""));
    }

    [Test]
    public void WhatTheServerRefused_IsListed()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.BadRequest,
            """{"status":400,"errors":{"PasswordTooShort":["Passwords must be at least 6 characters."]}}""");
        using var __ = ctx;
        var cut = ctx.Render<RegisterForm>();

        cut.Find("input[type=email]").Change("ann@x.test");
        foreach (var password in cut.FindAll("input[type=password]")) password.Input("short");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice li").TextContent,
            Is.EqualTo("Passwords must be at least 6 characters.")));
    }
}
