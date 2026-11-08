using LouisAgent.Choices;
using LouisAgent.Packs.Leads.Sending;
using LouisAgent.Packs.Leads.Tools;

namespace LouisAgent.Packs.Leads.Tests;

/// <summary>
/// The one tool (F112): it refuses a lead with a part missing and says which, sends a whole one with the clock's UTC
/// time, and turns the sender's failure into text for the model.
/// </summary>
public class LeadToolsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 40, 0, TimeSpan.Zero);

    /// <summary>A sender that keeps what it was given, or refuses with the message it was told to.</summary>
    private sealed class RecordingSender(string? refuseWith = null) : ILeadSender
    {
        public bool Mails => true;

        public List<Lead> Sent { get; } = [];

        public Task SendAsync(Lead lead, CancellationToken cancellationToken)
        {
            if (refuseWith is not null) throw new HttpRequestException(refuseWith);
            Sent.Add(lead);
            return Task.CompletedTask;
        }
    }

    /// <summary>A clock that never moves, so the answer's time can be asserted.</summary>
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static LeadTools Tools(RecordingSender sender) => new(sender, new FixedClock());

    /// <summary>Without a name, a contact or a want, nothing is sent and the model hears what to ask for.</summary>
    [TestCase("", "082 000 0000", "a bakery site", "name")]
    [TestCase("Thandi", " ", "a bakery site", "reach them")]
    [TestCase("Thandi", "082 000 0000", "", "what they want")]
    public async Task AnIncompleteLead_IsRefused_AndSaysWhatIsMissing(string name, string contact, string wants, string asksFor)
    {
        var sender = new RecordingSender();

        string answer = await Tools(sender).SendLead(name, contact, wants);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Not sent:").And.Contain(asksFor));
            Assert.That(sender.Sent, Is.Empty);
        });
    }

    /// <summary>A whole lead is sent with its three fields trimmed and the clock's time, and the answer says so.</summary>
    [Test]
    public async Task AWholeLead_IsSent_WithTheClocksTime()
    {
        var sender = new RecordingSender();

        string answer = await Tools(sender).SendLead(" Thandi ", "082 000 0000", "a bakery in Paarl, a site with an agent ");

        Assert.Multiple(() =>
        {
            Assert.That(sender.Sent, Has.Count.EqualTo(1));
            Assert.That(sender.Sent[0], Is.EqualTo(new Lead("Thandi", "082 000 0000", "a bakery in Paarl, a site with an agent", Now)));
            Assert.That(answer, Does.StartWith("Sent to Louis at 2026-11-08 12:40:00Z: Thandi, 082 000 0000"));
        });
    }

    /// <summary>The sender's failure comes back as text with its reason, never as a throw.</summary>
    [Test]
    public async Task ASendersFailure_IsTextForTheModel()
    {
        var sender = new RecordingSender(refuseWith: "422 Unprocessable Entity");

        string answer = await Tools(sender).SendLead("Thandi", "082 000 0000", "a bakery site");

        Assert.That(answer, Does.StartWith("Not sent: the mail service answered 422 Unprocessable Entity"));
    }

    /// <summary>With the log sender the model hears the truth: the lead was not mailed, only logged.</summary>
    [Test]
    public async Task TheLogSender_SaysItWasNotMailed()
    {
        var tools = new LeadTools(new LogLeadSender(Microsoft.Extensions.Logging.Abstractions.NullLogger<LogLeadSender>.Instance), new FixedClock());

        string answer = await tools.SendLead("Thandi", "082 000 0000", "a bakery site");

        Assert.That(answer, Does.StartWith("Not mailed: this instance has no mail key"));
    }

    /// <summary>The toolset's wire name is what a session asks for.</summary>
    [Test]
    public void TheToolset_IsNamedLeads() => Assert.That(LeadsToolset.Leads.Wire(), Is.EqualTo("leads"));
}
