using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Components.Chat.Header.Menu;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Chat.Header;

/// <summary>
/// The profile's menu over the seam (CL30): a guest on a site with accounts gets sign in and register; a signed-in
/// person gets their name, their profile and sign out; a site that registered nothing gets the guest's menu as it
/// always was.
/// </summary>
public class ProfileMenuTests
{
    [Test]
    public void WithNoAccountServices_ItIsTheGuestsMenu_AsBefore()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ProfileMenu>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Guest"));
            Assert.That(cut.FindAll("a[href=login]"), Is.Empty);
            Assert.That(cut.FindAll("button.dropdown-item").Select(b => b.TextContent.Trim()),
                Does.Contain("Profile").And.Contain("Sign out"));
        });
    }

    [Test]
    public void AGuest_OnASiteWithAccounts_GetsSignInAndRegister()
    {
        using var ctx = Context(new FakeAccountMenu { Enabled = true });

        var cut = ctx.Render<ProfileMenu>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("a[href=login]").TextContent.Trim(), Is.EqualTo("Sign in"));
            Assert.That(cut.Find("a[href=register]").TextContent.Trim(), Is.EqualTo("Create an account"));
            Assert.That(cut.FindAll("button.dropdown-item").Select(b => b.TextContent.Trim()),
                Does.Not.Contain("Sign out"));
        });
    }

    [Test]
    public void ASignedInPerson_SeesTheirName_TheirProfile_AndSignsOutThroughTheSeam()
    {
        var menu = new FakeAccountMenu
        {
            Enabled = true,
            SignedIn = true,
            Name = "Ann",
            Email = "ann@x.test"
        };
        using var ctx = Context(menu);

        var cut = ctx.Render<ProfileMenu>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find(".profile-button").TextContent, Is.EqualTo("A"));
            Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Ann"));
            Assert.That(cut.Find(".profile-who small").TextContent, Is.EqualTo("ann@x.test"));
            Assert.That(cut.Find("a[href=profile]").TextContent.Trim(), Is.EqualTo("Profile"));
        });
        cut.FindAll("button.dropdown-item").Single(b => b.TextContent.Contains("Sign out")).Click();
        cut.WaitForAssertion(() => Assert.That(menu.SignedOut, Is.True));
        Assert.That(menu.Loaded, Is.True, "the menu asked who's here once it drew");
    }

    [Test]
    public void WhenWhoIsHereChanges_TheMenuDrawsAgain()
    {
        var menu = new FakeAccountMenu { Enabled = true };
        using var ctx = Context(menu);
        var cut = ctx.Render<ProfileMenu>();
        cut.WaitForElement("a[href=login]");

        menu.SignedIn = true;
        menu.Name = "Ann";
        menu.Email = "ann@x.test";
        cut.InvokeAsync(menu.RaiseChanged);

        cut.WaitForAssertion(() => Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Ann")));
    }

    private static BunitContext Context(IAccountMenu menu)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(menu);
        return ctx;
    }

    /// <summary>The seam as a test scripts it: whatever state the test sets, and a note of what the menu asked.</summary>
    private sealed class FakeAccountMenu : IAccountMenu
    {
        public bool Enabled { get; init; }
        public bool SignedIn { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string SignInLabel => "Sign in";
        public string RegisterLabel => "Create an account";
        public bool Loaded { get; private set; }
        public bool SignedOut { get; private set; }

        public event Action? Changed;

        public Task LoadAsync()
        {
            Loaded = true;
            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            SignedOut = true;
            return Task.CompletedTask;
        }

        public void RaiseChanged() => Changed?.Invoke();
    }
}
