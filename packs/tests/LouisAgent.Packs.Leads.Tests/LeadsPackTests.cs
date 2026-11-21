using LouisAgent.Choices;
using LouisAgent.Packs.Leads.Sending;
using LouisAgent.Packs.Leads.Tools;

namespace LouisAgent.Packs.Leads.Tests;

/// <summary>
/// The pack is one toolset with its guide (F112): off until LEADS_TO is set, the log sender without a key and Resend
/// with one, and its settings read from the environment with their defaults.
/// </summary>
public class LeadsPackTests
{
    /// <summary>Its name, its toolset and its guide are the pack's own, and the guide says to ask before sending.</summary>
    [Test]
    public void TheGuide_ComesFromThePack()
    {
        var pack = new LeadsPack(new LeadsOptions());

        using var reader = new StreamReader(pack.Prompts.GetFileInfo(pack.Toolsets[0].GuideFile!).CreateReadStream());
        string guide = reader.ReadToEnd();

        Assert.Multiple(() =>
        {
            Assert.That(pack.Name, Is.EqualTo(LeadsToolset.Leads.Wire()));
            Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { LeadsToolset.Leads.Wire() }));
            Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/leads.md"));
            Assert.That(guide, Does.Contain("ask if you may send it").And.Contain("never send twice"));
        });
    }

    /// <summary>Without LEADS_TO the toolset says what it lacks; with it, it builds the one tool.</summary>
    [Test]
    public void ItNeedsTheOwnersAddress()
    {
        var off = new LeadsPack(new LeadsOptions()).Toolsets[0];
        var on = new LeadsPack(new LeadsOptions { To = "louis@example.test" }).Toolsets[0];

        Assert.Multiple(() =>
        {
            Assert.That(off.MissingSettings(null!), Is.EqualTo(new[] { "LEADS_TO" }));
            Assert.That(on.MissingSettings(null!), Is.Empty);
            Assert.That(on.ToolTypes, Is.EqualTo(new[] { typeof(LeadTools) }));
            Assert.That(on.CreateTools(null!).Single(), Is.TypeOf<LeadTools>());
        });
    }

    /// <summary>No key, the log sender; a key, Resend.</summary>
    [Test]
    public void TheKey_PicksTheSender()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LeadsPack.Sender(new LeadsOptions { To = "a@b.test" }, null, null), Is.TypeOf<LogLeadSender>());
            Assert.That(LeadsPack.Sender(new LeadsOptions { To = "a@b.test", ApiKey = "re_x" }, null, null), Is.TypeOf<ResendLeadSender>());
        });
    }

    /// <summary>The settings read from the environment, with the sender's default when none is given.</summary>
    [Test]
    public void TheOptions_ReadTheEnvironment()
    {
        var variables = new Dictionary<string, string?>
        {
            ["LEADS_TO"] = " louis@example.test ",
            ["RESEND_API_KEY"] = "re_x"
        };

        var options = LeadsOptions.FromEnvironment(name => variables.GetValueOrDefault(name));

        Assert.Multiple(() =>
        {
            Assert.That(options.To, Is.EqualTo("louis@example.test"));
            Assert.That(options.From, Is.EqualTo("guide@louis-agent.dev"));
            Assert.That(options.ApiKey, Is.EqualTo("re_x"));
        });
    }
}
