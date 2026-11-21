using System.Net;
using LouisAgent.Web.Accounts.Components;

namespace LouisAgent.Web.Accounts.Tests.Components;

/// <summary>The password field hides what's typed until asked, and hands every keystroke to the form.</summary>
public class PasswordFieldTests
{
    [Test]
    public void TheButton_ShowsAndHidesThePassword()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK);
        using var __ = ctx;
        var cut = ctx.Render<PasswordField>(p => p.Add(f => f.Label, "Password"));

        Assert.That(cut.Find("input").GetAttribute("type"), Is.EqualTo("password"));
        cut.Find("button").Click();
        Assert.That(cut.Find("input").GetAttribute("type"), Is.EqualTo("text"));
        Assert.That(cut.Find("button").GetAttribute("aria-label"), Is.EqualTo("Hide the password"));
    }

    [Test]
    public void WhatIsTyped_ReachesTheForm()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK);
        using var __ = ctx;
        string value = "";
        var cut = ctx.Render<PasswordField>(p =>
            p.Add(f => f.Label, "Password").Add(f => f.ValueChanged, v => value = v));

        cut.Find("input").Input("secret");

        Assert.That(value, Is.EqualTo("secret"));
    }
}
